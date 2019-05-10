using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Snapper.Json.Nunit
{
    internal class CalingTestInfo
    {
        public MethodBase Method { get; set; }
        public string FileName { get; set; }
    }

    internal static class NUnitTestHelper
    {
        public static CalingTestInfo GetCallingTestInfo()
        {
            var stackTrace = new StackTrace(2, true);
            foreach (var stackFrame in stackTrace.GetFrames() ?? new StackFrame[0])
            {
                var method = stackFrame.GetMethod();

                if (IsNUnitTestMethod(method))
                    return new CalingTestInfo { Method = method, FileName = stackFrame.GetFileName() };

                var asyncMethod = GetMethodBaseOfAsyncMethod(method);
                if (IsNUnitTestMethod(asyncMethod))
                    return new CalingTestInfo { Method = asyncMethod, FileName = stackFrame.GetFileName() }; ;
            }

            throw new InvalidOperationException(
                "Snapshots can only be created from classes decorated with the [Test] attribute");
        }

        private static bool IsNUnitTestMethod(MemberInfo method)
            => method?.GetCustomAttributes(typeof(TestAttribute), true).Any() ?? false;

        private static MethodBase GetMethodBaseOfAsyncMethod(MemberInfo asyncMethod)
        {
            var generatedType = asyncMethod?.DeclaringType;
            var originalClass = generatedType?.DeclaringType;
            if (originalClass == null)
                return null;

            var matchingMethods =
                from methodInfo in originalClass.GetMethods()
                let attr = methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>()
                where attr != null && attr.StateMachineType == generatedType
                select methodInfo;

            return matchingMethods.SingleOrDefault();
        }
    }
}
