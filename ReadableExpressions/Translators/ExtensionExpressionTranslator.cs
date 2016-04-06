namespace AgileObjects.ReadableExpressions.Translators
{
    using System;
    using System.Linq.Expressions;

    internal class ExtensionExpressionTranslator : ExpressionTranslatorBase
    {
        public ExtensionExpressionTranslator(Func<Expression, TranslationContext, string> globalTranslator)
            : base(globalTranslator, ExpressionType.Extension)
        {
        }

        public override string Translate(Expression expression, TranslationContext context)
        {
            return expression.ToString();
        }
    }
}