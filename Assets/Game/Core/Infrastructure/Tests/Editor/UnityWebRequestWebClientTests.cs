using NUnit.Framework;

namespace Infrastructure.Tests.Editor
{
    public sealed class UnityWebRequestWebClientTests
    {
        [Test]
        public void BuildAbsoluteUrl_WithRelativePath_CombinesWithBaseUrl()
        {
            var result = UnityWebRequestWebClient.BuildAbsoluteUrl("https://example.com/api/v1/", "rewards/grant");
            Assert.That(result, Is.EqualTo("https://example.com/api/v1/rewards/grant"));
        }

        [Test]
        public void BuildAbsoluteUrl_WithLeadingSlashRelativePath_CombinesWithBaseUrl()
        {
            var result = UnityWebRequestWebClient.BuildAbsoluteUrl("https://example.com/api/v1", "/wheel/spin");
            Assert.That(result, Is.EqualTo("https://example.com/api/v1/wheel/spin"));
        }

        [Test]
        public void BuildAbsoluteUrl_WithAbsoluteUrl_ReturnsInputUrl()
        {
            var result = UnityWebRequestWebClient.BuildAbsoluteUrl("https://example.com/api/v1", "https://other.host/ping");
            Assert.That(result, Is.EqualTo("https://other.host/ping"));
        }
    }
}
