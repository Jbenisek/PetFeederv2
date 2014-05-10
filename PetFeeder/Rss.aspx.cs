using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;

namespace PetFeeder
{
    public partial class Rss : System.Web.UI.Page
    {
        private string sFeedType;
        private string AdoptionFeedUrl;
        private string LostFeedUrl;
        private string FoundFeedUrl;
        private string SiteUrl;
        
        protected void Page_Load(object sender, EventArgs e) {
            sFeedType = Request.QueryString["type"];
            AdoptionFeedUrl = ConfigurationManager.AppSettings["AdoptionFeedUrl"];
            FoundFeedUrl = ConfigurationManager.AppSettings["FoundFeedUrl"];
            LostFeedUrl = ConfigurationManager.AppSettings["LostFeedUrl"];
            SiteUrl = ConfigurationManager.AppSettings["SiteUrl"];
            ProcessRSS();
        }

        protected void ProcessRSS() {
            XDocument petFeed;
            
            switch (sFeedType){
                case "adoption":
                    petFeed = XDocument.Load(AdoptionFeedUrl);
                    break;
                case "lost":
                    petFeed = XDocument.Load(LostFeedUrl);
                    break;
                case "found":
                    petFeed = XDocument.Load(FoundFeedUrl);
                    break;
                default:
                    petFeed = XDocument.Load(AdoptionFeedUrl);
                    break;
            }
            
            XDocument rssFeed = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement("rss",
                    new XAttribute("version", "2.0"),
                    new XElement("channel",
                        new XElement("title", "Pet Feed"),
                        new XElement("description", "Pet Feed"),
                        new XElement("link", SiteUrl.ToString()),
                        new XElement("lastBuildDate", DateTime.Now.ToString("ddd, dd MMM YYYY HH:mm:ss zzz")),
                        new XElement("pubDate", DateTime.Now.ToString("ddd, dd MMM YYYY HH:mm:ss zzz")),
                        new XElement("ttl", "1800"))));

            var data = from animal in petFeed.Descendants("DataFeedAnimal")
                       select new
                       {
                           AnimalId = (string)animal.Element("AnimalID"),
                           Name = (string)animal.Element("DistinguishingFeatures"),
                           Type = (string)animal.Element("Type"),
                           PrimaryPhotoUrl = (string)animal.Element("PrimaryPhotoUrl"),
                           EditDate = (string)animal.Element("EditDateTime"),
                           AdoptionSummary = (string)animal.Element("AdoptionSummary"),
                           Breed = (string)animal.Element("Breed"),
                           Colour = (string)animal.Element("Colour"),
                           PhysicalLocation = (string)animal.Element("LostFoundLocation"),
                           PhysicalLocationZip = (string)animal.Element("PhysicalLocationZip"),
                           HealthChecked = (string)animal.Element("HealthChecked"),
                           ShelterBuddyID = (string)animal.Element("ShelterBuddyID")
                       };

            data = data.OrderByDescending(a => a.EditDate).AsEnumerable();

            foreach (var animal in data)
            {
                string description = "<img src=\"" + animal.PrimaryPhotoUrl + "\" width=\"300\" /> " + animal.AdoptionSummary;
                rssFeed.Element("rss")
                    .Element("channel")
                    .Add(new XElement("item",
                        new XElement("title", animal.Type + ": " + animal.Name),
                        new XElement("description", description, animal.PhysicalLocation + ": " + animal.PhysicalLocationZip),
                        new XElement("link", SiteUrl + "/adopt-me?id=" + animal.ShelterBuddyID),
                        new XElement("guid",
                            new XAttribute("isPermaLink", "true"),
                            SiteUrl + "/adopt-me?id=" + animal.ShelterBuddyID),
                        new XElement("pubDate", animal.EditDate)));
            }

            Response.ContentType = "text/xml";
            Response.ContentEncoding = System.Text.Encoding.UTF8;
            rssFeed.Save(Response.Output);
        }
    }
}