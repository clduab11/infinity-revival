using System;
using System.Reflection;
using NUnit.Framework;
using Praxen.Game.Application;
using Praxen.Game.Domain;
using Praxen.Game.Infrastructure;

namespace Praxen.Game.Tests.EditMode
{
    public sealed class AssemblyBoundaryTests
    {
        [Test]
        public void CompiledDomainAssemblyHasNoEngineOrOtherGameDependencies()
        {
            AssertReferences(typeof(DomainAssemblyMarker).Assembly, "Praxen.Game.Domain");
        }

        [Test]
        public void CompiledApplicationAssemblyOnlyDependsInward()
        {
            AssertReferences(typeof(ApplicationStateMachine).Assembly,
                "Praxen.Game.Application", "Praxen.Game.Domain");
        }

        [Test]
        public void CompiledInfrastructureAssemblyOnlyDependsOnApplicationAndDomain()
        {
            AssertReferences(typeof(BoundedDiagnosticsSink).Assembly, "Praxen.Game.Infrastructure",
                "Praxen.Game.Application", "Praxen.Game.Domain");
        }

        private static void AssertReferences(Assembly assembly, string expectedName,
            params string[] allowedGameNames)
        {
            Assert.That(assembly.GetName().Name, Is.EqualTo(expectedName));
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                Assert.That(reference.Name, Does.Not.StartWith("UnityEngine"), assembly.FullName);
                Assert.That(reference.Name, Does.Not.StartWith("UnityEditor"), assembly.FullName);
                if (reference.Name.StartsWith("Praxen.Game.", StringComparison.Ordinal))
                    Assert.That(allowedGameNames, Does.Contain(reference.Name), assembly.FullName);
            }
        }
    }
}
