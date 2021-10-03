using System;

namespace RoslynBug
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class FromAnotherAssemblyAttribute : Attribute
    {
        public FromAnotherAssemblyAttribute(int argument) { }
    }
}
