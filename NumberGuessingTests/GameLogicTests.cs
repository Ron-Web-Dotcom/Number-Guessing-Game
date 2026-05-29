using System;
using NUnit.Framework;

namespace Number_Guessing
{
    [TestFixture]
    public class GameLogicTests
    {
        // ── GetProximity ─────────────────────────────────────────────────────────

        [Test]
        public void GetProximity_ExactAnswer_BurningHot()
        {
            Assert.AreEqual("BURNING HOT", GameLogic.GetProximity(15, 15, 1, 30));
        }

        [Test]
        public void GetProximity_WithinFivePercent_BurningHot()
        {
            // range=100, 5% = 5 units → distance of 4 should be BURNING HOT
            Assert.AreEqual("BURNING HOT", GameLogic.GetProximity(46, 50, 1, 100));
        }

        [Test]
        public void GetProximity_WithinFifteenPercent_Hot()
        {
            // range=100, 10% distance
            Assert.AreEqual("Hot!", GameLogic.GetProximity(40, 50, 1, 100));
        }

        [Test]
        public void GetProximity_WithinThirtyFivePercent_Warm()
        {
            // range=100, 25% distance
            Assert.AreEqual("Warm", GameLogic.GetProximity(25, 50, 1, 100));
        }

        [Test]
        public void GetProximity_WithinSixtyPercent_Cold()
        {
            // range=100, 45% distance
            Assert.AreEqual("Cold", GameLogic.GetProximity(5, 50, 1, 100));
        }

        [Test]
        public void GetProximity_OverSixtyPercent_FreezingCold()
        {
            // range=100, 70% distance
            Assert.AreEqual("Freezing cold", GameLogic.GetProximity(1, 80, 1, 100));
        }

        [Test]
        public void GetProximity_EqualMinMax_ReturnsOnIt()
        {
            Assert.AreEqual("On it!", GameLogic.GetProximity(5, 5, 5, 5));
        }

        // ── BuildFallbackHint ────────────────────────────────────────────────────

        [Test]
        public void BuildFallbackHint_Higher_ContainsHigher()
        {
            string hint = GameLogic.BuildFallbackHint("higher", int.MaxValue);
            StringAssert.Contains("higher", hint);
        }

        [Test]
        public void BuildFallbackHint_Lower_ContainsLower()
        {
            string hint = GameLogic.BuildFallbackHint("lower", int.MaxValue);
            StringAssert.Contains("lower", hint);
        }

        [Test]
        public void BuildFallbackHint_ThreeLeft_ShowsCount()
        {
            string hint = GameLogic.BuildFallbackHint("higher", 3);
            StringAssert.Contains("3", hint);
        }

        [Test]
        public void BuildFallbackHint_OneLeft_SingularGuess()
        {
            string hint = GameLogic.BuildFallbackHint("lower", 1);
            StringAssert.Contains("1 guess left", hint);
        }

        [Test]
        public void BuildFallbackHint_FourLeft_NoCount()
        {
            // Count only shown when ≤ 3 remaining
            string hint = GameLogic.BuildFallbackHint("higher", 4);
            Assert.IsFalse(hint.Contains("left"));
        }

        [Test]
        public void BuildFallbackHint_UnlimitedRemaining_NoCount()
        {
            string hint = GameLogic.BuildFallbackHint("higher", int.MaxValue);
            Assert.IsFalse(hint.Contains("left"));
        }

        // ── GetDailySeed / GetDailyNumber ────────────────────────────────────────

        [Test]
        public void GetDailySeed_SameDate_SameSeed()
        {
            var date = new DateTime(2026, 5, 29);
            Assert.AreEqual(GameLogic.GetDailySeed(date), GameLogic.GetDailySeed(date));
        }

        [Test]
        public void GetDailySeed_DifferentDates_DifferentSeeds()
        {
            var d1 = new DateTime(2026, 5, 29);
            var d2 = new DateTime(2026, 5, 30);
            Assert.AreNotEqual(GameLogic.GetDailySeed(d1), GameLogic.GetDailySeed(d2));
        }

        [Test]
        public void GetDailyNumber_SameDate_SameNumber()
        {
            var date = new DateTime(2026, 5, 29);
            int n1 = GameLogic.GetDailyNumber(1, 30, date);
            int n2 = GameLogic.GetDailyNumber(1, 30, date);
            Assert.AreEqual(n1, n2);
        }

        [Test]
        public void GetDailyNumber_AlwaysInRange()
        {
            var date = new DateTime(2026, 5, 29);
            int n = GameLogic.GetDailyNumber(1, 30, date);
            Assert.GreaterOrEqual(n, 1);
            Assert.LessOrEqual(n, 30);
        }

        [Test]
        public void GetDailyNumber_DifferentDates_CanDiffer()
        {
            // Not guaranteed to differ every adjacent day, but across a full month they must
            bool anyDiff = false;
            for (int day = 1; day <= 28; day++)
            {
                int a = GameLogic.GetDailyNumber(1, 30, new DateTime(2026, 1, day));
                int b = GameLogic.GetDailyNumber(1, 30, new DateTime(2026, 1, day + 1));
                if (a != b) { anyDiff = true; break; }
            }
            Assert.IsTrue(anyDiff);
        }

        // ── BinarySearchMid ──────────────────────────────────────────────────────

        [Test]
        public void BinarySearchMid_EvenRange_ReturnsLowerMid()
        {
            Assert.AreEqual(5, GameLogic.BinarySearchMid(1, 10));
        }

        [Test]
        public void BinarySearchMid_OddRange_ReturnsMid()
        {
            Assert.AreEqual(15, GameLogic.BinarySearchMid(1, 30));
        }

        [Test]
        public void BinarySearchMid_SingleElement_ReturnsThatElement()
        {
            Assert.AreEqual(7, GameLogic.BinarySearchMid(7, 7));
        }

        [Test]
        public void BinarySearchMid_ConvergesOnHardRange()
        {
            // Simulate binary search on 1-100; must resolve in ≤ 7 steps
            int target = 73, low = 1, high = 100, steps = 0;
            while (low <= high)
            {
                int mid = GameLogic.BinarySearchMid(low, high);
                steps++;
                if (mid == target) break;
                if (mid < target) low = mid + 1;
                else high = mid - 1;
            }
            Assert.LessOrEqual(steps, 7);
        }

        // ── ComputeAverage ───────────────────────────────────────────────────────

        [Test]
        public void ComputeAverage_NoWins_ReturnsZero()
        {
            Assert.AreEqual(0.0, GameLogic.ComputeAverage(0, 0));
        }

        [Test]
        public void ComputeAverage_CorrectCalculation()
        {
            // 15 total attempts across 3 wins = average 5.0
            Assert.AreEqual(5.0, GameLogic.ComputeAverage(15, 3));
        }

        [Test]
        public void ComputeAverage_RoundsToOneDecimal()
        {
            // 10 / 3 = 3.333... → rounded to 3.3
            Assert.AreEqual(3.3, GameLogic.ComputeAverage(10, 3));
        }
    }
}
