﻿using FakeItEasy;
using FluentAssertions;
using MrCMS.Entities.Documents.Web;
using MrCMS.Shortcodes;
using MrCMS.Website;
using Ninject.MockingKernel;
using Xunit;

namespace MrCMS.Tests.Shortcodes
{
    public class ElementRendererManagerTests
    {
        private ElementRendererManager _elementRendererManager;

        public ElementRendererManagerTests()
        {
            _elementRendererManager = new ElementRendererManager();
        }

        [Fact]
        public void DefaultFormRenderer_GetElementRenderer_ShouldReturnTheResultOfTheTypeOfAPropertyFromTheKernel()
        {
            var mockingKernel = new MockingKernel();
            var formElementRenderer = A.Fake<IFormElementRenderer<TextBox>>();
            mockingKernel.Bind<IFormElementRenderer<TextBox>>()
                         .ToMethod(context => formElementRenderer)
                         .InSingletonScope();
            MrCMSApplication.OverrideKernel(mockingKernel);

            var renderer = _elementRendererManager.GetElementRenderer(new TextBox());

            renderer.Should().Be(formElementRenderer);
        }
    }
}