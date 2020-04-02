using System;
using System.Collections.Generic;

namespace ScmBackup
{
    internal enum ApiAuthenticationTypeEnum
    {
        BasicAuth,
        OAuth
    }

    internal enum ScmAuthenticationType
    {
        Https,
        Ssh
    }

    /// <summary>
    /// Configuration data to get the repositories of user X from hoster Y
    /// (subclass for Config)
    /// </summary>
    internal class ConfigSource
    {
        /// <summary>
        /// title of this config source (must be unique)
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// name of the hoster
        /// </summary>
        public string Hoster { get; set; }

        /// <summary>
        /// user type (e.g. user/team)
        /// </summary>
        public string Type { get; set; }

        public ApiAuthenticationTypeEnum? ApiAuthenticationType { get; set; }

        /// <summary>
        /// user name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// user name for authentication
        /// (can be a different than the user whose repositories are backed up)
        /// </summary>
        public string AuthName { get; set; }

        /// <summary>
        /// password for authentication
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// consumer key for OAuth authentication
        /// </summary>
        public string OAuthConsumerKey { get; set; }

        /// <summary>
        /// consumer secret for OAuth authentication
        /// </summary>
        public string OAuthConsumerSecret { get; set; }

        public ScmAuthenticationType? ScmAuthenticationType { get; set; }

        /// <summary>
        /// list of repository names which should be ignored
        /// </summary>
        public List<string> IgnoreRepos { get; set; }



        public bool IsAuthenticated
        {
            get
            {
                return ApiAuthenticationType switch
                {
                    var x when x == ApiAuthenticationTypeEnum.BasicAuth || !x.HasValue 
                        => !string.IsNullOrWhiteSpace(AuthName) 
                           && !string.IsNullOrWhiteSpace(Password),
                    ApiAuthenticationTypeEnum.OAuth 
                        => !string.IsNullOrWhiteSpace(OAuthConsumerKey) 
                           && !string.IsNullOrWhiteSpace(OAuthConsumerSecret),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var source = obj as ConfigSource;

            if (source == null)
            {
                return false;
            }

            return (source.Title == this.Title);
        }

        public override int GetHashCode()
        {
            return this.Title.GetHashCode();
        }
    }
}
