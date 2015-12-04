﻿using Cecilifier.Core.AST;
using Microsoft.CodeAnalysis;

namespace Cecilifier.Core.Extensions
{
	static class ISymboExtensions
	{
		public static bool IsDefinedInCurrentType<T>(this T method, IVisitorContext ctx) where T : ISymbol
		{
			return method.ContainingAssembly == ctx.SemanticModel.Compilation.Assembly;
		}
	}
}
