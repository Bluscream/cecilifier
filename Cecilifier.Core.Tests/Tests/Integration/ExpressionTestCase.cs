﻿using Cecilifier.Core.Tests.Framework;
using NUnit.Framework;

namespace Cecilifier.Core.Tests.Integration
{
    public class ExpressionTestCase : IntegrationResourceBasedTest
    {
        [Test]
        public void TestParameterAssignment()
        {
            AssertResourceTest(@"Expressions/ParameterAssignment");
        }

        [Test]
        public void TestLocalVariableAssignment()
        {
            AssertResourceTest(@"Expressions/LocalVariableAssignment");
        }

        [Test]
        public void TestMultipleLocalVariableAssignment()
        {
            AssertResourceTestWithExplicitExpectation(@"Expressions/MultipleLocalVariableAssignment", "System.Void MultipleLocalVariableAssignment::Method(System.Int32,System.String)");
        }

        [Test]
        public void TestLocalVariableInitialization()
        {
            AssertResourceTest(@"Expressions/LocalVariableInitialization");
        }

        [Test]
        public void TestBox()
        {
            AssertResourceTest(@"Expressions/Box");
        }

        [Test]
        public void TestStackalloc()
        {
            AssertResourceTest(@"Expressions/Operators/stackalloc");
        }
        
        [Test]
        public void TestAdd()
        {
            AssertResourceTest(@"Expressions/Operators/Add");
        }

        [Test]
        public void TestAdd2()
        {
            AssertResourceTestWithExplicitExpectation(@"Expressions/Operators/Add2", "System.Void AddOperations2::IntegerString(System.String,System.Int32)");
        }

        [Test]
        public void TestEquals()
        {
            AssertResourceTest(@"Expressions/Operators/Equals");
        }

        [Test]
        public void TestLessThan()
        {
            AssertResourceTest(@"Expressions/Operators/LessThan");
        }

        [Test]
        public void TestTernaryOperator()
        {
            AssertResourceTestBinary(@"Expressions/Operators/Ternary", TestKind.Integration);
        }

        [Test]
        public void TestTypeInferenceInDeclarations()
        {
            AssertResourceTestWithExplicitExpectation(@"Expressions/TypeInferenceInDeclarations", "System.Void TypeInferenceInDeclarations::Foo()");
        }

        [Test]
        [Ignore("REQUIRES TRANSFORMATION")]
        public void TestValueTypeAddress()
        {
            AssertResourceTest(@"Expressions/ValueTypeAddress");
        }

        [Test]
        [Ignore("newing primitives are not supported.")]
        public void TestNewPrimitive()
        {
            AssertResourceTest(@"Expressions/NewPrimitive");
        }

        [Test]
        public void TestNewCustom()
        {
            AssertResourceTest(@"Expressions/NewCustom");
        }

        [Test]
        public void TestNewSingleDimentionArray()
        {
            AssertResourceTest(@"Expressions/NewSingleDimentionArray");
        }
    }
}
