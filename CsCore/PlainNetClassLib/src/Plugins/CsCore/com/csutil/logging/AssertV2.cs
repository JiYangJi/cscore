using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace com.csutil {
    public static class AssertV2 {

        private const string BR = "\r\n";

        private static void Assert(bool condition, string errorMsg, object[] args) {
            args = new StackTrace(2, true).AddTo(args);
            if (!condition) { Fail(errorMsg, args); }
        }

        private static void Fail(string errorMsg, object[] args) {
            Debugger.Break();
            var e = Log.e(errorMsg, args);
            if (throwExeptionIfAssertionFails) { throw e; }
        }

        public static void Throws<T>(Action actionThatShouldThrowAnException) where T : Exception {
            try { actionThatShouldThrowAnException(); } catch (Exception e) {
                if (e is T) { return; } // its the expected exception
                throw; // its an unexpected exception, so rethrow it
            }
            throw new ThrowsException("No exception of type " + typeof(T) + " was thrown!");
        }

        [Serializable]
        public class ThrowsException : Exception {
            public ThrowsException() : base() { }
            public ThrowsException(string message) : base(message) { }
            public ThrowsException(string message, Exception innerException) : base(message, innerException) { }
        }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        public static void IsTrue(bool condition, string errorMsg, params object[] args) {
            Assert(condition, "Assert.IsTrue() FAILED: " + errorMsg, args);
        }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        public static void IsFalse(bool condition, string errorMsg, params object[] args) {
            Assert(!condition, "Assert.IsFalse() FAILED: " + errorMsg, args);
        }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        public static void IsEqualToPersisted(string id, object o, params object[] args) {
            args = new StackTrace(1, true).AddTo(args);
            var persisted = IoC.inject.GetOrAddSingleton<PersistedRegression>(o);
            persisted.AssertEqualToPersisted(id, o).ContinueWith((Task t) => {
                if (t.Exception != null) { Fail(t.Exception.Message, args); }
            });
        }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        public static void IsNull(object o, string varName, params object[] args) {
            string errorMsg = "Assert.IsNull(" + varName + ") FAILED";
            Assert(o == null, errorMsg, args);
        }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        public static void IsNotNull(object o, string varName, params object[] args) {
            string errorMsg = "Assert.IsNotNull(" + varName + ") FAILED";
            Assert(o != null, errorMsg, args);
        }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        public static void AreEqual<T>(IEquatable<T> expected, IEquatable<T> actual, string varName = "", params object[] args) {
            var errorMsg = "Assert.AreEqual() FAILED: expected " +
                varName + "= " + expected + " NOT equal to actual " + varName + "= " + actual;
            Assert(expected.Equals(actual), errorMsg, args);
        }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        public static void AreNotEqual<T>(IEquatable<T> expected, IEquatable<T> actual, string varName = "", params object[] args) {
            var isNotSameRef = !ReferenceEquals(expected, actual);
            Assert(isNotSameRef, "Assert.AreNotEqual() FAILED: " + varName + " is same reference (expected " + expected + " == actual " + actual + " )", args);
            if (isNotSameRef) {
                var errorMsg = "Assert.AreNotEqual() FAILED: expected " + varName + "= " + expected + " IS equal to actual " + varName + "= " + actual;
                Assert(!Equals(expected, actual), errorMsg, args);
            }
        }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        public static void AreEqualJson(object a, object b) {
            if (ReferenceEquals(a, b)) { throw new ArgumentException("Both references pointed to the same object"); }
            var expected = JsonWriter.GetWriter().Write(a);
            var actual = JsonWriter.GetWriter().Write(b);
            AreEqual(expected, actual);
        }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        public static void AreNotEqualLists<T>(IEnumerable<T> expected, IEnumerable<T> actual, string varName = "", params object[] args) {
            string msg1 = "Assert.AreNotEqual() FAILED: " + varName + " is same reference (expected == actual)";
            Assert(expected != actual, msg1, args);
            string msg2 = "Assert.AreNotEqual() FAILED: expected " + varName + "= " + expected + " IS equal to actual " + varName + "= " + actual;
            Assert(!expected.SequenceEqual(actual), msg2, args);
        }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        public static void IsInRange(double lowerBound, double value, double upperBound, string varName, params object[] args) {
            if (upperBound < lowerBound) { throw Log.e($"Invalid bounds: (upperBound){upperBound} < {lowerBound}(lowerBound)"); }
            Assert(lowerBound <= value, "Assert.IsInRange() FAILED: " + varName + "=" + value + " is BELOW lower bound=" + lowerBound, args);
            Assert(value <= upperBound, "Assert.IsInRange() FAILED: " + varName + "=" + value + " is ABOVE upper bound=" + upperBound, args);
        }

        public static StopwatchV2 TrackTiming([CallerMemberName] string methodName = null) { return new StopwatchV2(methodName).StartV2(); }

        [Conditional("DEBUG"), Conditional("ENFORCE_ASSERTIONS")]
        public static void AssertUnderXms(this Stopwatch self, int maxTimeInMs, params object[] args) {
            var ms = self.ElapsedMilliseconds;
            int p = (int)(ms * 100f / maxTimeInMs);
            var errorText = GetMethodName(self) + " took " + p + "% (" + ms + "ms) longer then allowed (" + maxTimeInMs + "ms)!";
            Assert(IsUnderXms(self, maxTimeInMs), errorText, args);
        }

        private static string GetMethodName(Stopwatch s) {
            if (s is StopwatchV2 sV2) { return sV2.methodName; } else { return new StackFrame(2).GetMethodName(false); }
        }

        public static bool IsUnderXms(this Stopwatch self, int maxTimeInMs) { return self.ElapsedMilliseconds <= maxTimeInMs; }

        private static object syncLock = new object();
        public static bool throwExeptionIfAssertionFails = false;

        public static void ThrowExeptionIfAssertionFails(Action taskToExecute) {
            ThrowExeptionIfAssertionFails(true, taskToExecute);
        }

        public static void ThrowExeptionIfAssertionFails(bool shouldThrow, Action taskToExecute) {
            lock (syncLock) {
                var oldVal = throwExeptionIfAssertionFails;
                throwExeptionIfAssertionFails = shouldThrow;
                taskToExecute();
                throwExeptionIfAssertionFails = oldVal;
            }
        }

    }
}