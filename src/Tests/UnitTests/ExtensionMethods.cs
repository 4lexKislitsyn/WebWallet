using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace UnitTests
{
    /// <summary>
    /// Extension methods for unit tests.
    /// </summary>
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Check that result is instance of <typeparamref name="T"/> and return valid status code.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static T IsResult<T>(this IActionResult result, HttpStatusCode? status = null) where T : class
        {
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<T>(result);
            var convertedResult = result as T;
            if (status.HasValue)
            {
                int? statusCode = null;
                switch (convertedResult)
                {
                    case ObjectResult objectResult:
                        statusCode = objectResult.StatusCode;
                        break;
                    case StatusCodeResult codeResult:
                        statusCode = codeResult.StatusCode;
                        break;
                    default:
                        Assert.Fail("Cannot check passed status code.");
                        break;
                }
                Assert.AreEqual((int)status.Value, statusCode);
            }
            return convertedResult;
        }
        /// <summary>
        /// Check that <see cref="ObjectResult.Value"/> is instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectResult"></param>
        /// <returns></returns>
        public static T HasContent<T>(this ObjectResult objectResult)
        {
            Assert.IsNotNull(objectResult.Value);
            Assert.IsInstanceOf<T>(objectResult.Value);
            return (T)objectResult.Value;
        }
        /// <summary>
        /// Check that result is instance of <see cref="CreatedResult"/> and return in content instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <param name="locationEndsWith"></param>
        /// <returns></returns>
        public static T IsCreatedWithContent<T>(this IActionResult result, string locationEndsWith)
            => IsCreatedWithContent<T>(result, x => locationEndsWith);
        /// <summary>
        /// Check that result is instance of <see cref="CreatedResult"/> and return in content instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="result"></param>
        /// <param name="locationEndsWith"></param>
        /// <returns></returns>
        public static T IsCreatedWithContent<T>(this IActionResult result, Func<T, string> getLocationEndsWith = null)
        {
            var createdResult = IsResult<CreatedResult>(result, HttpStatusCode.Created);
            Assert.IsFalse(string.IsNullOrWhiteSpace(createdResult.Location));
            var content = HasContent<T>(createdResult);
            if (getLocationEndsWith != null)
            {
                Assert.IsTrue(createdResult.Location.EndsWith(getLocationEndsWith.Invoke(content)));
            }
            return content;
        }

        /// <summary>
        /// Check that result is instance of <typeparamref name="TResult"/> and has content of type <typeparamref name="TContent"/>.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TContent"></typeparam>
        /// <param name="result"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static TContent IsResultWithContent<TResult, TContent>(this IActionResult result, HttpStatusCode? code = null) where TResult : ObjectResult
        {
            var objectResult = result.IsResult<TResult>(code);
            return objectResult.HasContent<TContent>();
        }
    }
}
