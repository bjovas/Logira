﻿// ReSharper disable InconsistentNaming

using System;
using NUnit.Framework;
using Brevity;
using Shouldly;

namespace Logira.Tests
{
    [TestFixture]
    public class IssueBuilderTests
    {
        private IssueBuilder _builder;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            Jira.Configure("your jira site", "user", "pass");
        }

        [SetUp]
        public void Setup()
        {
            _builder = new IssueBuilder();
        }

        [Test]
        public void Create_fails_when_projectKey_is_not_set()
        {
            Should.Throw<InvalidOperationException>(() =>
                _builder
                    .Summary("Summary")
                    .Create())
                .Message.ShouldContain("Project");
        }

        [Test]
        public void Create_fails_when_summary_is_not_set()
        {
            Should.Throw<InvalidOperationException>(() =>
                _builder
                    .Project("TST")
                    .Create())
                .Message.ShouldContain("Summary");
        }

        [Test]
        public void Summary_is_not_truncated_when_less_than_max()
        {
            Jira.MaxSummaryLength = 10;
            var summary = "*".Repeat(10);
            _builder.Summary(summary);
            _builder.CreateRemoteIssue().summary.ShouldBe(summary);
        }

        [Test]
        public void Summary_is_truncated_when_maxchars_is_exceeded()
        {
            Jira.MaxSummaryLength = 10;
            var summary = "*".Repeat(20);
            _builder.Summary(summary);
            var remoteIssue = _builder.CreateRemoteIssue();
            remoteIssue.summary.ShouldBe(summary.Truncate(Jira.MaxSummaryLength));
            remoteIssue.description.ShouldContain(summary);
        }

        [Test]
        public void Environment_is_set()
        {
            const string environment = "the environment";
            _builder
                .Summary("test")
                .Project("test")
                .Environment(environment);

            var remoteIssue = _builder.CreateRemoteIssue();

            remoteIssue.environment.ShouldBe(environment);
        }

        [Test]
        public void Environment_is_set_from_server()
        {
            _builder
                .Summary("test")
                .Project("test")
                .Environment().FromServer();

            var remoteIssue = _builder.CreateRemoteIssue();

            remoteIssue.environment.ShouldContain(Environment.CurrentDirectory);
        }

        [Test]
        public void Creating_component_is_nut_supported_by_soap_service()
        {
            _builder
                .Summary("test")
                .Project("test")
                .Component("test component foo");

            Should.Throw<NotSupportedException>(() => _builder.CreateRemoteComponents());
        }

        [Test]
        public void Issue_has_correct_url()
        {
            var issue = new Issue("TST-123");
            var jiraUrl = "http://the-jira-site.com";
            Jira.Configure(jiraUrl, "user", "pass");
            issue.Url.ShouldBe(jiraUrl + "/browse/" + issue.Key);
        }

        [Test]
        public void Inner_exceptions_are_added_to_description()
        {
            try
            {
                throw new ArgumentException("inner inner");
            }
            catch (Exception innerInnerException)
            {
                try
                {
                    throw new ApplicationException("inner", innerInnerException);
                }
                catch (Exception innerException)
                {
                    try
                    {
                        throw new InvalidOperationException("outer", innerException);
                    }
                    catch (Exception outerException)
                    {
                        _builder
                            .Project("TST")
                            .Summary("Summary")
                            .Description(outerException);

                        var remoteIssue = _builder.CreateRemoteIssue();

                        remoteIssue.description.ShouldContain(innerInnerException.Message);
                        remoteIssue.description.ShouldContain(innerException.Message);
                        remoteIssue.description.ShouldContain(outerException.Message);
                    }
                }
            }
        }

        [Test]
        public void Rich_text_with_macros()
        {
            _builder
                .Description(new CodeMacro {Code = "some code", Title = "some title"})
                .Description(new QuoteMacro {Quote = "some quote"});

            var remoteIssue = _builder.CreateRemoteIssue();

            remoteIssue.description.ShouldContain(@"{code:title=some title}some code{code}");
            remoteIssue.description.ShouldContain(@"{quote}some quote{quote}");
        }
    }
}

// ReSharper restore InconsistentNaming