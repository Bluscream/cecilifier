﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Ceciifier.Core.Tests.Framework.AssemblyDiff
{
	class AssemblyComparer
	{
		private readonly AssemblyDefinition first;
		private readonly AssemblyDefinition second;

		public AssemblyComparer(string pathToFirst, string pathToSecond)
		{
			first= AssemblyDefinition.ReadAssembly(pathToFirst);
			second = AssemblyDefinition.ReadAssembly(pathToSecond);
		}

		public string First
		{
			get { return first.MainModule.FullyQualifiedName ; }
		}

		public string Second
		{
			get { return second.MainModule.FullyQualifiedName; }
		}

		public bool Compare(IAssemblyDiffVisitor visitor)
		{
			if (first.Modules.Count != second.Modules.Count)
			{
				if (!visitor.VisitModules(first, second)) return false;
			}

			//TODO: Correcly handle multiple modules.
			var sourceModule = first.MainModule;
			var targetModule = second.MainModule;

			var ret = true;
			ISet<TypeDefinition> firstTypes = new HashSet<TypeDefinition>(sourceModule.Types);
			foreach (var sourceType in firstTypes.Where(t => t.FullName != "<Module>"))
			{
				var typeVisitor = visitor.VisitType(sourceType);
				if (typeVisitor == null) continue;

				var targetType = targetModule.Types.SingleOrDefault(t => t.FullName == sourceType.FullName);
				if (targetType == default(TypeDefinition))
				{
					if (!typeVisitor.VisitMissing(sourceType, targetModule)) return false;
					continue;
				}

				ret = ret && CheckTypeAttributes(typeVisitor, sourceType, targetType);
				//ret = ret && CheckTypeGenericInformation(typeVisitor, sourceType, targetType);
				ret = ret && CheckTypeMembers(typeVisitor, sourceType, targetType);
				ret = ret && CheckTypeInheritance(typeVisitor, sourceType, targetType);
				ret = ret && CheckImplementedInterfaces(typeVisitor, sourceType, targetType);
			}

			//TODO: Check missing classes
			return ret;
		}

		private static bool CheckImplementedInterfaces(ITypeDiffVisitor typeVisitor, TypeDefinition sourceType, TypeDefinition targetType)
		{
            if (sourceType.Interfaces.Count != targetType.Interfaces.Count) return false;

		    return sourceType.Interfaces.Except(targetType.Interfaces, new InterfaceComparer()).Any() == false;
		}

		private static bool CheckTypeInheritance(ITypeDiffVisitor typeVisitor, TypeDefinition source, TypeDefinition target)
		{
			if (target.BaseType == null && source.BaseType == null) return true;
			if (target.BaseType != null && (source.BaseType.FullName == target.BaseType.FullName)) return true;
			
			return typeVisitor.VisitBaseType(source.BaseType.Resolve(), target);
		}


		private static bool CheckTypeMembers(ITypeDiffVisitor typeVisitor, TypeDefinition source, TypeDefinition target)
		{
			if (!CheckFields(typeVisitor, source, target)) return false;

			if (!CheckMethods(typeVisitor, source, target)) return false;

			//foreach (var sourceEvent in source.Events)
			//{
			//}

			//foreach (var sourcePropertie in source.Properties)
			//{
			//}

			return true;
		}

		private static bool CheckMethods(ITypeDiffVisitor typeVisitor, TypeDefinition source, TypeDefinition target)
		{
			var ret = true;
			var targetMethods = target.Methods.ToDictionary(m => m.FullName);
			foreach (var sourceMethod in source.Methods)
			{
				var memberVisitor = typeVisitor.VisitMember(sourceMethod);
				if (memberVisitor == null) continue;

				if (!CheckTypeMember(memberVisitor, sourceMethod, target, targetMethods))
				{
					ret = false;
					continue;
				}

				var targetMethod = targetMethods[sourceMethod.FullName];
				if (sourceMethod.ReturnType.FullName != targetMethod.ReturnType.FullName)
				{
					if (!memberVisitor.VisitReturnType(sourceMethod, targetMethod)) return false;
				}

				if (sourceMethod.Attributes != targetMethod.Attributes)
				{
					if (!memberVisitor.VisitAttributes(sourceMethod, targetMethod)) return false;
				}

				if (!CheckMethodBody(memberVisitor, sourceMethod, targetMethod))
				{
					ret = false;
				}
			}

			return ret;
		}

		private static bool CheckMethodBody(IMethodDiffVisitor visitor, MethodDefinition source, MethodDefinition target)
		{
			if (source.HasBody != target.HasBody)
			{
				return visitor.VisitBody(source, target);
			}

			if (source.Body == null) return true;

			Func<Instruction, bool> ignoreNops = i => i.OpCode != OpCodes.Nop;
			var targetInstructions = target.Body.Instructions.Where(ignoreNops).GetEnumerator();
			foreach (var instruction in source.Body.Instructions.Where(ignoreNops))
			{
				if (!targetInstructions.MoveNext())
				{
					return visitor.VisitBody(source, target, instruction);
				}

				if (!EqualOrEquivalent(instruction, targetInstructions.Current))
				{
					return visitor.VisitBody(source, target, instruction);
				}
			}

			if (targetInstructions.MoveNext())
			{
				return visitor.VisitBody(source, target, targetInstructions.Current);
			}

			return true;
		}

		private static bool EqualOrEquivalent(Instruction instruction, Instruction current)
		{
			if (instruction.OpCode == current.OpCode) return true;

			switch (instruction.OpCode.Code)
			{
				case Code.Ldarg_0: return current.OpCode.Code == Code.Ldarg && (int) current.Operand == 0;
				case Code.Ldarg_1: return current.OpCode.Code == Code.Ldarg && (int) current.Operand == 1;
				case Code.Ldarg_2: return current.OpCode.Code == Code.Ldarg && (int) current.Operand == 2;
				case Code.Ldarg_3: return current.OpCode.Code == Code.Ldarg && (int) current.Operand == 3;

				case Code.Ldloc_0: return current.OpCode.Code == Code.Ldloc && VarIndex(current.Operand) == 0;
				case Code.Ldloc_1: return current.OpCode.Code == Code.Ldloc && VarIndex(current.Operand) == 1;
				case Code.Ldloc_2: return current.OpCode.Code == Code.Ldloc && VarIndex(current.Operand) == 2;
				case Code.Ldloc_3: return current.OpCode.Code == Code.Ldloc && VarIndex(current.Operand) == 3;

				case Code.Stloc_0: return current.OpCode.Code == Code.Stloc && VarIndex(current.Operand) == 0;
				case Code.Stloc_1: return current.OpCode.Code == Code.Stloc && VarIndex(current.Operand) == 1;
				case Code.Stloc_2: return current.OpCode.Code == Code.Stloc && VarIndex(current.Operand) == 2;
				case Code.Stloc_3: return current.OpCode.Code == Code.Stloc && VarIndex(current.Operand) == 3;
			}
			return false;

		}

		private static int VarIndex(object operand)
		{
			return ((VariableDefinition) operand).Index;
		}

		private static bool CheckFields(ITypeDiffVisitor typeVisitor, TypeDefinition source, TypeDefinition target)
		{
			var targetFields = target.Fields.ToDictionary(f => f.Name);
			foreach (var sourceMember in source.Fields)
			{
				var memberVisitor = typeVisitor.VisitMember(sourceMember);
				if (memberVisitor == null) continue;

				if (!CheckTypeMember(memberVisitor, sourceMember, target, targetFields)) continue;

				var targetField = targetFields[sourceMember.Name];
				if (sourceMember.FieldType.FullName != targetField.FieldType.FullName)
				{
					if (!memberVisitor.VisitFieldType(sourceMember, targetField)) return false;
				}

				if (sourceMember.Attributes != targetField.Attributes)
				{
					if (!memberVisitor.VisitAttributes(sourceMember, targetField)) return false;
				}
			}
			return true;
		}


		private static bool CheckTypeMember<T>(IMemberDiffVisitor memberVisitor, IMemberDefinition sourceMember, TypeDefinition target, IDictionary<string, T> targetMembers) where T : IMemberDefinition
		{
			if (!targetMembers.ContainsKey(sourceMember.FullName))
			{
				return memberVisitor.VisitMissing(sourceMember, target);
			}

			var targetMember = targetMembers[sourceMember.FullName];
			if (sourceMember.FullName != targetMember.FullName)
			{
				if (!memberVisitor.VisitName(sourceMember, targetMember)) return false;
			}

			if (sourceMember.DeclaringType.FullName != targetMember.DeclaringType.FullName)
			{
				if (!memberVisitor.VisitDeclaringType(sourceMember, target)) return false;
			}

			return true;
		}

		private static bool CheckTypeAttributes(ITypeDiffVisitor typeVisitor, TypeDefinition source, TypeDefinition target)
		{
			return source.Attributes == target.Attributes || typeVisitor.VisitAttributes(source, target);
		}
	}

    internal class InterfaceComparer : IEqualityComparer<TypeReference>
    {
        public bool Equals(TypeReference x, TypeReference y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(TypeReference obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
