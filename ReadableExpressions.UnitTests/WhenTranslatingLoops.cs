﻿namespace AgileObjects.ReadableExpressions.UnitTests
{
    using System;
    using System.Linq.Expressions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WhenTranslatingLoops
    {
        [TestMethod]
        public void ShouldTranslateAnInfiniteLoop()
        {
            Expression<Action> writeLine = () => Console.WriteLine();
            var loop = Expression.Loop(writeLine.Body);

            var translated = loop.ToReadableString();

            const string EXPECTED = @"
while (true)
{
    Console.WriteLine();
}";
            Assert.AreEqual(EXPECTED.TrimStart(), translated);
        }

        [TestMethod]
        public void ShouldTranslateALoopWithABreakStatement()
        {
            var intVariable = Expression.Variable(typeof(int), "i");
            var intGreaterThanTwo = Expression.GreaterThan(intVariable, Expression.Constant(2));
            var breakLoop = Expression.Break(Expression.Label());
            var ifGreaterThanTwoBreak = Expression.IfThen(intGreaterThanTwo, breakLoop);
            Expression<Action> writeLine = () => Console.WriteLine();
            var incrementVariable = Expression.Increment(intVariable);
            var loopBody = Expression.Block(ifGreaterThanTwoBreak, writeLine.Body, incrementVariable);
            var loop = Expression.Loop(loopBody, breakLoop.Target);

            var translated = loop.ToReadableString();

            const string EXPECTED = @"
while (true)
{
    if (i > 2)
    {
        break;
    }

    Console.WriteLine();
    ++i;
}";
            Assert.AreEqual(EXPECTED.TrimStart(), translated);
        }

        [TestMethod]
        public void ShouldTranslateALoopWithAContinueStatement()
        {
            var intVariable = Expression.Variable(typeof(int), "i");
            var intLessThanThree = Expression.LessThan(intVariable, Expression.Constant(3));
            var incrementVariable = Expression.Increment(intVariable);
            var continueLoop = Expression.Continue(Expression.Label());
            var incrementAndContinue = Expression.Block(incrementVariable, continueLoop);
            var ifLessThanThreeContinue = Expression.IfThen(intLessThanThree, incrementAndContinue);
            Expression<Action> writeFinished = () => Console.Write("Finished!");
            var returnFromLoop = Expression.Return(Expression.Label());
            var loopBody = Expression.Block(ifLessThanThreeContinue, writeFinished.Body, returnFromLoop);
            var loop = Expression.Loop(loopBody, returnFromLoop.Target, continueLoop.Target);

            var translated = loop.ToReadableString();

            const string EXPECTED = @"
while (true)
{
    if (i < 3)
    {
        ++i;
        continue;
    }

    Console.Write(""Finished!"");
    return;
}";
            Assert.AreEqual(EXPECTED.TrimStart(), translated);
        }
    }
}
