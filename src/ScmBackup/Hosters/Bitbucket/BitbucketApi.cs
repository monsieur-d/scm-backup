using Newtonsoft.Json;
using ScmBackup.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Authentication;
using SharpBucket.V2;
using SharpBucket.V2.Pocos;

namespace ScmBackup.Hosters.Bitbucket
{
    internal class BitbucketApi : IHosterApi
    {
        private readonly SharpBucketV2 sharpBucket = new SharpBucketV2();

        public BitbucketApi(IHttpRequest request)
        {

        }

        public List<HosterRepository> GetRepositoryList(ConfigSource source)
        {
            var list = new List<HosterRepository>();
            string className = this.GetType().Name;

            if (source.IsAuthenticated)
            {
                if(source.ApiAuthenticationType == ApiAuthenticationTypeEnum.OAuth)
                    sharpBucket.OAuth2ClientCredentials(source.OAuthConsumerKey, source.OAuthConsumerSecret);
                else
            {
                    sharpBucket.BasicAuthentication(source.AuthName, source.Password);
                    }
                }

            List<Repository> repositories;
            if (source.Type.ToLower() == "user")
                {
                var userEndpoint = sharpBucket.UsersEndPoint(source.Name);
                repositories = userEndpoint.ListRepositories();
                }
            else
            {
                var teamResource = sharpBucket.TeamsEndPoint().TeamResource(source.Name);
                repositories = teamResource.ListRepositories();
            }

            foreach (var repository in repositories)
            {
                        ScmType type;
                switch (repository.scm.ToLower())
                        {
                            case "hg":
                                type = ScmType.Mercurial;
                                break;
                            case "git":
                                type = ScmType.Git;
                                break;
                            default:
                        throw new InvalidOperationException(string.Format(Resource.ApiInvalidScmType, repository.full_name));
                        }

                var scmAuthenticationType = source.ScmAuthenticationType.ToString().ToLower();

                var clone = repository.links.clone.First(r => r.name == "https");
                var cloneUrl = clone.href;

                var repo = new HosterRepository(repository.full_name, repository.slug, cloneUrl, type); 

                repo.SetPrivate(repository.is_private ?? false);

                if (repository.has_wiki ?? false)
                        {
                    string wikiUrl = cloneUrl + "/wiki";
                            repo.SetWiki(true, wikiUrl.ToString());
                        }

                        // TODO: Issues

                        list.Add(repo);
                    }

            return list;
        }
    }
}
