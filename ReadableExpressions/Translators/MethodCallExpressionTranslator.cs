namespace AgileObjects.ReadableExpressions.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;
    using NetStandardPolyfills;

    internal class MethodCallExpressionTranslator : ExpressionTranslatorBase
    {
        private readonly SpecialCaseHandlerBase[] _specialCaseHandlers;

        internal MethodCallExpressionTranslator(IndexAccessExpressionTranslator indexAccessTranslator)
            : base(ExpressionType.Call, ExpressionType.Invoke)
        {
            _specialCaseHandlers = new SpecialCaseHandlerBase[]
            {
                new InvocationExpressionHandler(GetMethodCall),
                new StringConcatenationHandler(),
                new IndexedPropertyHandler(indexAccessTranslator)
            };
        }

        public override string Translate(Expression expression, TranslationContext context)
        {
            var specialCaseHandler = _specialCaseHandlers.FirstOrDefault(sch => sch.AppliesTo(expression));

            if (specialCaseHandler != null)
            {
                return specialCaseHandler.Translate(expression, context);
            }

            var methodCall = (MethodCallExpression)expression;
            IEnumerable<Expression> methodArguments;
            var methodCallSubject = GetMethodCallSubject(methodCall, context, out methodArguments);

            return GetMethodCall(
                methodCallSubject,
                methodCall.Method,
                methodArguments,
                expression,
                context);
        }

        public static string GetMethodCallSubject(MethodCallExpression methodCall, TranslationContext context)
        {
            IEnumerable<Expression> arguments;

            return GetMethodCallSubject(methodCall, context, out arguments);
        }

        private static string GetMethodCallSubject(
            MethodCallExpression methodCall,
            TranslationContext context,
            out IEnumerable<Expression> arguments)
        {
            if (methodCall.Object == null)
            {
                return GetStaticMethodCallSubject(methodCall, context, out arguments);
            }

            arguments = methodCall.Arguments;

            return context.Translate(methodCall.Object);
        }

        private static string GetStaticMethodCallSubject(
            MethodCallExpression methodCall,
            TranslationContext context,
            out IEnumerable<Expression> arguments)
        {
            if (methodCall.Method.IsExtensionMethod())
            {
                var subject = methodCall.Arguments.First();
                arguments = methodCall.Arguments.Skip(1).ToArray();

                return context.Translate(subject);
            }

            arguments = methodCall.Arguments;

            // ReSharper disable once PossibleNullReferenceException
            return methodCall.Method.DeclaringType.GetFriendlyName();
        }

        private string GetMethodCall(
            string subject,
            MethodInfo method,
            IEnumerable<Expression> arguments,
            Expression originalMethodCall,
            TranslationContext context)
        {
            return GetMethodCall(
                subject,
                new BclMethodInfoWrapper(method),
                arguments,
                originalMethodCall,
                context);
        }

        internal string GetMethodCall(
            string subject,
            IMethodInfo method,
            IEnumerable<Expression> arguments,
            Expression originalMethodCall,
            TranslationContext context)
        {
            var separator = context.IsPartOfMethodCallChain(originalMethodCall)
                ? Environment.NewLine + ".".Indented()
                : ".";

            return subject + separator + GetMethodCall(method, arguments, context);
        }

        internal string GetMethodCall(
            IMethodInfo method,
            IEnumerable<Expression> arguments,
            TranslationContext context)
        {
            var parametersString = context.TranslateParameters(arguments, method).WithParentheses();
            var genericArguments = GetGenericArgumentsIfNecessary(method, context);

            return method.Name + genericArguments + parametersString;
        }

        private static string GetGenericArgumentsIfNecessary(IMethodInfo method, TranslationContext context)
        {
            if (!method.IsGenericMethod)
            {
                return null;
            }

            var methodGenericDefinition = method.GetGenericMethodDefinition();
            var genericParameterTypes = methodGenericDefinition.GetGenericArguments().ToList();

            if (context.Settings.UseImplicitGenericParameters)
            {
                RemoveSuppliedGenericTypeParameters(
                    methodGenericDefinition.GetParameters().Select(p => p.ParameterType),
                    genericParameterTypes);
            }

            if (!genericParameterTypes.Any())
            {
                return null;
            }

            var argumentNames = method
                .GetGenericArguments()
                .Select(a => a.GetFriendlyName())
                .Where(name => name != null)
                .ToArray();

            if (!argumentNames.Any())
            {
                return null;
            }

            return $"<{string.Join(", ", argumentNames)}>";
        }

        private static void RemoveSuppliedGenericTypeParameters(
            IEnumerable<Type> types,
            ICollection<Type> genericParameterTypes)
        {
            foreach (var type in types.Select(t => t.IsByRef ? t.GetElementType() : t))
            {
                if (type.IsGenericParameter && genericParameterTypes.Contains(type))
                {
                    genericParameterTypes.Remove(type);
                }

                if (type.IsGenericType())
                {
                    RemoveSuppliedGenericTypeParameters(type.GetGenericArguments(), genericParameterTypes);
                }
            }
        }

        #region Helper Classes

        private abstract class SpecialCaseHandlerBase
        {
            private readonly Func<Expression, bool> _applicabilityTester;

            protected SpecialCaseHandlerBase(Func<Expression, bool> applicabilityTester)
            {
                _applicabilityTester = applicabilityTester;
            }

            public bool AppliesTo(Expression expression)
            {
                return _applicabilityTester.Invoke(expression);
            }

            public abstract string Translate(Expression expression, TranslationContext context);
        }

        private class StringConcatenationHandler : SpecialCaseHandlerBase
        {
            public StringConcatenationHandler()
                : base(IsStringConcatCall)
            {
            }

            private static bool IsStringConcatCall(Expression expression)
            {
                var method = ((MethodCallExpression)expression).Method;

                return method.IsStatic &&
                      (method.DeclaringType == typeof(string)) &&
                      (method.Name == "Concat");
            }

            public override string Translate(Expression expression, TranslationContext context)
            {
                var methodCall = (MethodCallExpression)expression;

                return methodCall.Arguments.ToStringConcatenation(context);
            }
        }

        private class InvocationExpressionHandler : SpecialCaseHandlerBase
        {
            public delegate string MethodCallTranslator(
                string subject,
                MethodInfo method,
                IEnumerable<Expression> arguments,
                Expression originalMethodCall,
                TranslationContext context);

            private readonly MethodCallTranslator _methodCallTranslator;

            public InvocationExpressionHandler(MethodCallTranslator methodCallTranslator)
                : base(exp => exp.NodeType == ExpressionType.Invoke)
            {
                _methodCallTranslator = methodCallTranslator;
            }

            public override string Translate(Expression expression, TranslationContext context)
            {
                var invocation = (InvocationExpression)expression;
                var invocationSubject = context.Translate(invocation.Expression);

                if (invocation.Expression.NodeType == ExpressionType.Lambda)
                {
                    invocationSubject = invocationSubject.WithSurroundingParentheses();
                }

                var invocationMethod = invocation.Expression.Type.GetMethod("Invoke");

                return _methodCallTranslator.Invoke(
                    invocationSubject,
                    invocationMethod,
                    invocation.Arguments,
                    expression,
                    context);
            }
        }

        private class IndexedPropertyHandler : SpecialCaseHandlerBase
        {
            private readonly IndexAccessExpressionTranslator _indexAccessTranslator;

            public IndexedPropertyHandler(IndexAccessExpressionTranslator indexAccessTranslator)
                : base(IsIndexedPropertyAccess)
            {
                _indexAccessTranslator = indexAccessTranslator;
            }

            private static bool IsIndexedPropertyAccess(Expression expression)
            {
                var methodCall = (MethodCallExpression)expression;

                var property = methodCall
                    .Object?
                    .Type
                    .GetProperties()
                    .FirstOrDefault(p => p.GetAccessors().Contains(methodCall.Method));

                if (property == null)
                {
                    return false;
                }

                var propertyIndexParameters = property.GetIndexParameters();

                return propertyIndexParameters.Any();
            }

            public override string Translate(Expression expression, TranslationContext context)
            {
                var methodCall = (MethodCallExpression)expression;

                return _indexAccessTranslator.TranslateIndexAccess(
                    methodCall.Object,
                    methodCall.Arguments,
                    context);
            }
        }

        #endregion
    }
}