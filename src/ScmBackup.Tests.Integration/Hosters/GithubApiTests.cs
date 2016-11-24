﻿using ScmBackup.Hosters;
using ScmBackup.Hosters.Github;
using ScmBackup.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using Xunit;

namespace ScmBackup.Tests.Integration.Hosters
{
    public class GithubApiTests
    {
        [Fact]
        public void CallsGithubApi_UnauthenticatedUser()
        {
            var config = new ConfigSource();
            config.Hoster = "github";
            config.Type = "user";
            config.Name = "scm-backup-testuser";

            var logger = new FakeLogger();
            var request = new HttpRequest();

            var sut = new GithubApi(request, logger);

            var repoList = sut.GetRepositoryList(config);

            // HTTP status ok?
            Assert.Equal(HttpStatusCode.OK, sut.LastResult.Status);

            // at least one result?
            Assert.NotNull(repoList);
            Assert.True(repoList.Count > 0);

            // specific repo exists?
            var repo = repoList.Where(r => r.Name == "scm-backup-testuser#scm-backup").FirstOrDefault();
            Assert.NotNull(repo);
            Assert.False(string.IsNullOrWhiteSpace(repo.CloneUrl));
        }

        [Fact]
        public void CallsGithubApi_NonExistingUser_ThrowsException()
        {
            var config = new ConfigSource();
            config.Hoster = "github";
            config.Type = "user";
            config.Name = "scm-backup-testuser-does-not-exist";

            var logger = new FakeLogger();
            var request = new HttpRequest();

            var sut = new GithubApi(request, logger);

            List<HosterRepository> repoList;
            Assert.Throws<InvalidOperationException>(() => repoList = sut.GetRepositoryList(config));
        }

        [Fact]
        public void CallsGithubApi_AuthenticatedUser_InvalidPasswordThrowsException()
        {
            var config = new ConfigSource();
            config.Hoster = "github";
            config.Type = "user";
            config.Name = "scm-backup-testuser";
            config.AuthName = config.Name;
            config.Password = "invalid-password";

            var logger = new FakeLogger();
            var request = new HttpRequest();

            var sut = new GithubApi(request, logger);

            List<HosterRepository> repoList;
            Assert.Throws<AuthenticationException>(() => repoList = sut.GetRepositoryList(config)); 
        }

        [Fact]
        public void CallsGithubApi_AuthenticatedUser()
        {
            var config = new ConfigSource();
            config.Hoster = "github";
            config.Type = "user";
            config.Name = TestHelper.EnvVar("GithubApiTests_Name");
            config.AuthName = config.Name;
            config.Password = TestHelper.EnvVar("GithubApiTests_PW");

            var logger = new FakeLogger();
            var request = new HttpRequest();

            var sut = new GithubApi(request, logger);

            var repoList = sut.GetRepositoryList(config);

            // HTTP status ok?
            Assert.Equal(HttpStatusCode.OK, sut.LastResult.Status);

            // at least one result?
            Assert.NotNull(repoList);
            Assert.True(repoList.Count > 0);

            // specific repo exists?
            string expectedName = (config.Name + '#' + TestHelper.EnvVar("GithubApiTests_Repo")).ToLower();
            var repo = repoList.Where(r => r.Name == expectedName).FirstOrDefault();
            Assert.NotNull(repo);
            Assert.False(string.IsNullOrWhiteSpace(repo.CloneUrl));
        }
    }
}