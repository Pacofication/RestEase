﻿using Moq;
using RestEase;
using RestEase.Implementation;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RestEaseUnitTests.ImplementationBuilderTests
{
    public class MethodInfoTests
    {
        public interface IHasOverloads
        {
            [Get]
            Task FooAsync(string foo);

            [Get]
            Task FooAsync(int foo);
        }

        public interface IParent
        {
            [Get]
            Task FooAsync(string bar);
        }
        public interface IChild : IParent { }

        public interface IGenericParent<T>
        {
            [Get]
            Task FooAsync(T bar);
        }

        public interface IChildWithTwoGenericParents : IGenericParent<int>, IGenericParent<string> { }

        private readonly Mock<IRequester> requester = new Mock<IRequester>(MockBehavior.Strict);
        private readonly ImplementationBuilder builder = ImplementationBuilder.Instance;

        [Fact]
        public void GetsMethodInfoForOverloadedMethods()
        {
            var intOverload = Request<IHasOverloads>(x => x.FooAsync(1)).MethodInfo;
            var expectedIntOverload = typeof(IHasOverloads).GetTypeInfo().GetMethod("FooAsync", new Type[] { typeof(int) });
            Assert.Equal(expectedIntOverload, intOverload);

            var stringOverload = Request<IHasOverloads>(x => x.FooAsync("test")).MethodInfo;
            var expectedstringOverload = typeof(IHasOverloads).GetTypeInfo().GetMethod("FooAsync", new Type[] { typeof(string) });
            Assert.Equal(expectedstringOverload, stringOverload);
        }

        [Fact]
        public void GetsMethodInfoFromParentInterface()
        {
            var methodInfo = Request<IChild>(x => x.FooAsync("testy")).MethodInfo;
            var expected = typeof(IParent).GetTypeInfo().GetMethod("FooAsync");
            Assert.Equal(expected, methodInfo);
        }

        [Fact]
        public void GetsCorrectGenericMethod()
        {
            var intOverload = Request<IChildWithTwoGenericParents>(x => x.FooAsync(1)).MethodInfo;
            var expectedIntOverload = typeof(IGenericParent<int>).GetTypeInfo().GetMethod("FooAsync");
            Assert.Equal(expectedIntOverload, intOverload);

            var stringOverload = Request<IChildWithTwoGenericParents>(x => x.FooAsync("test")).MethodInfo;
            var expectedstringOverload = typeof(IGenericParent<string>).GetTypeInfo().GetMethod("FooAsync");
            Assert.Equal(expectedstringOverload, stringOverload);
        }

        private IRequestInfo Request<T>(Func<T, Task> selector)
        {
            var implementation = this.builder.CreateImplementation<T>(this.requester.Object);

            IRequestInfo requestInfo = null;

            this.requester.Setup(x => x.RequestVoidAsync(It.IsAny<IRequestInfo>()))
                .Callback((IRequestInfo r) => requestInfo = r)
                .Returns(Task.FromResult(false));

            selector(implementation);

            return requestInfo;
        }
    }
}
