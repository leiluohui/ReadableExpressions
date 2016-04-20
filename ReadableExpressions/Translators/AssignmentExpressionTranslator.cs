namespace AgileObjects.ReadableExpressions.Translators
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    internal class AssignmentExpressionTranslator : ExpressionTranslatorBase
    {
        private static readonly Dictionary<ExpressionType, string> _symbolsByNodeType =
            new Dictionary<ExpressionType, string>
            {
                [ExpressionType.AddAssign] = "+=",
                [ExpressionType.AddAssignChecked] = "+=",
                [ExpressionType.AndAssign] = "&=",
                [ExpressionType.Assign] = "=",
                [ExpressionType.DivideAssign] = "/=",
                [ExpressionType.ExclusiveOrAssign] = "^=",
                [ExpressionType.LeftShiftAssign] = "<<=",
                [ExpressionType.ModuloAssign] = @"%=",
                [ExpressionType.MultiplyAssign] = "*=",
                [ExpressionType.MultiplyAssignChecked] = "*=",
                [ExpressionType.OrAssign] = "|=",
                [ExpressionType.PowerAssign] = "**=",
                [ExpressionType.RightShiftAssign] = ">>=",
                [ExpressionType.SubtractAssign] = "-=",
                [ExpressionType.SubtractAssignChecked] = "-="
            };

        internal AssignmentExpressionTranslator(Translator globalTranslator)
            : base(globalTranslator, _symbolsByNodeType.Keys.ToArray())
        {
        }

        public override string Translate(Expression expression, TranslationContext context)
        {
            var assignment = (BinaryExpression)expression;
            var target = GetTranslation(assignment.Left, context);

            return GetAssignment(target, expression.NodeType, assignment.Right, context);
        }

        internal string GetAssignment(
            string target,
            ExpressionType assignmentType,
            Expression value,
            TranslationContext context)
        {
            var symbol = _symbolsByNodeType[assignmentType];
            var valueString = GetTranslation(value, context);

            switch (value.NodeType)
            {
                case ExpressionType.Assign:
                case ExpressionType.Block:
                    if (valueString.EndsWith(';'))
                    {
                        valueString = valueString.Substring(0, valueString.Length - 1);
                    }

                    if (!valueString.HasSurroundingParentheses())
                    {
                        valueString = valueString.WithSurroundingParentheses();
                    }

                    break;

                default:
                    valueString = valueString.WithoutSurroundingParentheses(value);
                    break;

            }

            return $"{target} {symbol} {valueString}";
        }
    }
}