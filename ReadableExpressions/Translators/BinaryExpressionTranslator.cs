namespace AgileObjects.ReadableExpressions.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    internal class BinaryExpressionTranslator : ExpressionTranslatorBase
    {
        private static readonly Dictionary<ExpressionType, string> _operatorsByNodeType =
            new Dictionary<ExpressionType, string>
            {
                [ExpressionType.Add] = "+",
                [ExpressionType.AddChecked] = "+",
                [ExpressionType.And] = "&",
                [ExpressionType.AndAlso] = "&&",
                [ExpressionType.Coalesce] = "??",
                [ExpressionType.Divide] = "/",
                [ExpressionType.Equal] = "==",
                [ExpressionType.ExclusiveOr] = "^",
                [ExpressionType.GreaterThan] = ">",
                [ExpressionType.GreaterThanOrEqual] = ">=",
                [ExpressionType.LeftShift] = "<<",
                [ExpressionType.LessThan] = "<",
                [ExpressionType.LessThanOrEqual] = "<=",
                [ExpressionType.Modulo] = "%",
                [ExpressionType.Multiply] = "*",
                [ExpressionType.MultiplyChecked] = "*",
                [ExpressionType.NotEqual] = "!=",
                [ExpressionType.Or] = "|",
                [ExpressionType.OrElse] = "||",
                [ExpressionType.Power] = "**",
                [ExpressionType.RightShift] = ">>",
                [ExpressionType.Subtract] = "-",
                [ExpressionType.SubtractChecked] = "-"
            };

        internal BinaryExpressionTranslator(Func<Expression, string> globalTranslator)
            : base(globalTranslator, _operatorsByNodeType.Keys.ToArray())
        {
        }

        public override string Translate(Expression expression)
        {
            var binaryExpression = (BinaryExpression)expression;
            var left = GetTranslation(binaryExpression.Left);
            var @operator = _operatorsByNodeType[expression.NodeType];
            var right = GetTranslation(binaryExpression.Right);

            return $"({left} {@operator} {right})";
        }
    }
}