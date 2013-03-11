﻿using FakeItEasy;
using FluentAssertions;
using MrCMS.Shortcodes;
using MrCMS.Tests.Stubs;
using Xunit;

namespace MrCMS.Tests.Shortcodes
{
    public class FormRenderingManagerTests
    {
        private FormRenderingManager _formRenderingManager;
        private IDefaultFormRenderer _defaultFormRenderer;
        private ICustomFormRenderer _customFormRenderer;

        public FormRenderingManagerTests()
        {
            _defaultFormRenderer = A.Fake<IDefaultFormRenderer>();
            _customFormRenderer = A.Fake<ICustomFormRenderer>();
            _formRenderingManager = new FormRenderingManager(_defaultFormRenderer, _customFormRenderer);
        }

        [Fact]
        public void FormRenderer_RenderForm_WhenFormDesignIsEmptyReturnsResultOfIDefaultFormRenderer()
        {
            var stubWebpage = new StubWebpage();
            A.CallTo(() => _defaultFormRenderer.GetDefault(stubWebpage)).Returns("test-default");

            var renderForm = _formRenderingManager.RenderForm(stubWebpage);

            renderForm.Should().Be("test-default");
        }

        [Fact]
        public void FormRenderer_RenderForm_IfWebpageIsNullReturnsEmptyString()
        {
            var renderForm = _formRenderingManager.RenderForm(null);

            renderForm.Should().Be("");
        }

        [Fact]
        public void FormRenderer_RenderForm_IfFormDesignHasValueReturnResultCustomRendererGetForm()
        {
            var stubWebpage = new StubWebpage {FormDesign = "form-design-data"};
            A.CallTo(() => _customFormRenderer.GetForm(stubWebpage)).Returns("custom-form");

            var renderForm = _formRenderingManager.RenderForm(stubWebpage);

            renderForm.Should().Be("custom-form");
        }
    }
}