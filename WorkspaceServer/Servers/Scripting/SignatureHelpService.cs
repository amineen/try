﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WorkspaceServer.Models.SingatureHelp;

// Adapted from https://github.com/OmniSharp/omnisharp-roslyn/blob/master/src/OmniSharp.Roslyn.CSharp/Services/Signatures/SignatureHelpService.cs

namespace WorkspaceServer.Servers.Scripting
{
    public class SignatureHelpService
    {
        public static async Task<SignatureHelpResponse> GetSignatureHelp(Document document, int position, Budget budget = null)
        {
            var invocation = await GetInvocation(document, position);

            var response = new SignatureHelpResponse();

            if (invocation == null)
            {
                return response;
            }
            // define active parameter by position
            foreach (var comma in invocation.Separators)
            {
                if (comma.Span.Start > invocation.Position)
                {
                    break;
                }

                response.ActiveParameter += 1;
            }

            // process all signatures, define active signature by types
            var signaturesSet = new HashSet<SignatureHelpItem>();
            var bestScore = int.MinValue;
            SignatureHelpItem bestScoredItem = null;

            var types = invocation.ArgumentTypes;
            foreach (var methodOverload in GetMethodOverloads(invocation.SemanticModel, invocation.Receiver))
            {
                var signature = BuildSignature(methodOverload);
                signaturesSet.Add(signature);

                var score = InvocationScore(methodOverload, types);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestScoredItem = signature;
                }
            }

            var signaturesList = signaturesSet.ToList();
            response.Signatures = signaturesList;
            response.ActiveSignature = signaturesList.IndexOf(bestScoredItem);

            return response;


        }
        

        private static async Task<InvocationContext> GetInvocation(Document document, int position)
        {
            var tree = await document.GetSyntaxTreeAsync();
            var root = await tree.GetRootAsync();
            var node = root.FindToken(position).Parent;

            // Walk up until we find a node that we're interested in.
            while (node != null)
            {
                switch (node)
                {
                    case InvocationExpressionSyntax invocation when invocation.ArgumentList.Span.Contains(position):
                    {
                        var semanticModel = await document.GetSemanticModelAsync();
                        return new InvocationContext(semanticModel, position, invocation.Expression, invocation.ArgumentList);
                    }
                    case ObjectCreationExpressionSyntax objectCreation when objectCreation.ArgumentList?.Span.Contains(position) ?? false:
                    {
                        var semanticModel = await document.GetSemanticModelAsync();
                        return new InvocationContext(semanticModel, position, objectCreation, objectCreation.ArgumentList);
                    }
                    case AttributeSyntax attributeSyntax when attributeSyntax.ArgumentList.Span.Contains(position):
                    {
                        var semanticModel = await document.GetSemanticModelAsync();
                        return new InvocationContext(semanticModel, position, attributeSyntax, attributeSyntax.ArgumentList);
                    }
                }

                node = node.Parent;
            }

            return null;
        }

        private static IEnumerable<IMethodSymbol> GetMethodOverloads(SemanticModel semanticModel, SyntaxNode node)
        {
            ISymbol symbol = null;
            var symbolInfo = semanticModel.GetSymbolInfo(node);
            if (symbolInfo.Symbol != null)
            {
                symbol = symbolInfo.Symbol;
            }
            else if (!symbolInfo.CandidateSymbols.IsEmpty)
            {
                symbol = symbolInfo.CandidateSymbols.First();
            }

            return symbol?.ContainingType == null 
                ? Array.Empty<IMethodSymbol>() 
                : symbol.ContainingType.GetMembers(symbol.Name).OfType<IMethodSymbol>();
        }

        private static int InvocationScore(IMethodSymbol symbol, IEnumerable<TypeInfo> types)
        {
            var parameters = GetParameters(symbol);
            if (parameters.Count() < types.Count())
            {
                return int.MinValue;
            }

            var score = 0;

            foreach (var (invocation, definition) in types.Zip(parameters, (i, d) => (i, d)))
            {
                if (invocation.ConvertedType == null)
                {
                    // 1 point for having a parameter
                    score += 1;
                }
                else if (invocation.ConvertedType.Equals(definition.Type))
                {
                    // 2 points for having a parameter and being
                    // the same type
                    score += 2;
                }
            }
         
            return score;
        }

        private static SignatureHelpItem BuildSignature(IMethodSymbol symbol)
        {
            var signature = new SignatureHelpItem
            {
                Documentation = symbol.GetDocumentationCommentXml(),
                Name = symbol.MethodKind == MethodKind.Constructor ? symbol.ContainingType.Name : symbol.Name,
                Label = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                Parameters = GetParameters(symbol).Select(parameter => new SignatureHelpParameter
                {
                    Name = parameter.Name,
                    Label = parameter.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    Documentation = parameter.GetDocumentationCommentXml()
                })
            };


            return signature;
        }

        private static IEnumerable<IParameterSymbol> GetParameters(IMethodSymbol methodSymbol)
        {
            return !methodSymbol.IsExtensionMethod ? methodSymbol.Parameters : methodSymbol.Parameters.RemoveAt(0);
        }
    }
}