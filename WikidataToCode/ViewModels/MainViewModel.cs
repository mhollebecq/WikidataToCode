using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WikidataToCode.Models;

namespace WikidataToCode.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IEnumerable<Property> _properties;
        public IEnumerable<Property> Properties
        {
            get { return _properties; }
            set
            {
                _properties = value;
                base.NotifyPropertyChanged();
            }
        }


        private SearchItem _selectedSearchItem;
        public SearchItem SelectedSearchItem
        {
            get { return _selectedSearchItem; }
            set
            {
                _selectedSearchItem = value;
                if (_selectedSearchItem != null)
                {
                    ShowInstanceOf(value.Id);
                    ShowProperties(value.Id);
                }
                base.NotifyPropertyChanged();
            }
        }

        private InstanceOfItem _selectedInstanceOfItem;
        public InstanceOfItem SelectedInstanceOfItem
        {
            get { return _selectedInstanceOfItem; }
            set
            {
                _selectedInstanceOfItem = value;
                if (_selectedInstanceOfItem != null)
                {
                    ShowExemples(value.Id);
                    BuildModel();
                }
                base.NotifyPropertyChanged();
            }
        }

        private IEnumerable<InstanceOfItem> instanceOfItems;
        public IEnumerable<InstanceOfItem> InstanceOfItems
        {
            get { return instanceOfItems; }
            set
            {
                instanceOfItems = value;
                base.NotifyPropertyChanged();
            }
        }

        private IEnumerable<SearchItem> _searchItems;
        public IEnumerable<SearchItem> SearchItems
        {
            get { return _searchItems; }
            set
            {
                _searchItems = value;
                base.NotifyPropertyChanged();
            }
        }


        private string _search;
        public string Search
        {
            get { return _search; }
            set
            {
                _search = value;
                DoSearch(_search);
                base.NotifyPropertyChanged();
            }
        }

        //https://www.wikidata.org/w/api.php?action=wbsearchentities&search=abc&language=en
        public MainViewModel()
        {
            //ShowInstanceOf("Q12772");
            //ShowExemples("Q6465");
        }

        public async Task ShowExemples(string id)
        {
            try
            {
                //SelectedSearchItem = null;
                using (HttpClient httpClient = new HttpClient())
                {
                    string url = "https://query.wikidata.org/sparql?query=";
                    url += Uri.EscapeUriString($"SELECT ?dpt WHERE {{ ?dpt wdt:P31 wd:{id}. }} LIMIT 50");
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url + "&language=fr"))//&format=json
                    {
                        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/sparql-results+json"));
                        var response = await httpClient.SendAsync(request);
                        string allResponse = await response.Content.ReadAsStringAsync();
                        List<string> exemplesId = new List<string>();
                        JObject rootResponseJson = Newtonsoft.Json.Linq.JObject.Parse(allResponse);
                        var results = rootResponseJson["results"];
                        foreach (var binding in results["bindings"])
                        {
                            var uri = binding["dpt"]["value"];
                            exemplesId.Add(uri.Value<string>().Replace("http://www.wikidata.org/entity/", string.Empty));
                        }
                        await GetAllData("fr", exemplesId.Take(50).ToArray());
                        //var searchJson = rootResponseJson["search"];
                        //List<SearchItem> searchItems = new List<SearchItem>();
                        //foreach (var searchItemJson in searchJson)
                        //{
                        //    var id = searchItemJson["id"];
                        //    var label = searchItemJson["label"];
                        //    var description = searchItemJson["description"];
                        //    searchItems.Add(new SearchItem((id ?? "").ToString(),
                        //        (label ?? "").ToString(),
                        //        (description ?? "").ToString()));
                        //}
                        //SearchItems = searchItems;

                    }
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.Message);
            }
        }

        private async Task/*Task<IEnumerable<(string id, string label)>>*/ GetAllData(string lang, params string[] ids)
        {
            List<string> names = new List<string>();
            List<(string name, string value)> labelsList = new List<(string name, string value)>();
            string id = string.Join("|", ids);
            try
            {
                //SelectedSearchItem = null;
                using (HttpClient httpClient = new HttpClient())
                {
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,
                        $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={id}&language=fr&format=json"))
                    {
                        var response = await httpClient.SendAsync(request);
                        string allResponse = await response.Content.ReadAsStringAsync();
                        JObject rootResponseJson = Newtonsoft.Json.Linq.JObject.Parse(allResponse);
                        var entities = rootResponseJson["entities"];
                        foreach (JProperty entity in entities)
                        {
                            var qName = entity.Name;
                            var labels = entity.Value["labels"];
                            if (labels != null)
                            {
                                var labelFr = labels["fr"];
                                if (labelFr!=null)
                                {
                                    names.Add(labelFr["value"].ToString());
                                }
                            }
                            var claims = entity.Value["claims"];
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
        }

        public async Task DoSearch(string search)
        {
            try
            {
                //SelectedSearchItem = null;
                using (HttpClient httpClient = new HttpClient())
                {
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://www.wikidata.org/w/api.php?action=wbsearchentities&search={search}&language=fr&format=json"))
                    {
                        var response = await httpClient.SendAsync(request);
                        string allResponse = await response.Content.ReadAsStringAsync();
                        JObject rootResponseJson = Newtonsoft.Json.Linq.JObject.Parse(allResponse);
                        var searchJson = rootResponseJson["search"];
                        List<SearchItem> searchItems = new List<SearchItem>();
                        foreach (var searchItemJson in searchJson)
                        {
                            var id = searchItemJson["id"];
                            var label = searchItemJson["label"];
                            var description = searchItemJson["description"];
                            searchItems.Add(new SearchItem((id ?? "").ToString(),
                                (label ?? "").ToString(),
                                (description ?? "").ToString()));
                        }
                        SearchItems = searchItems;

                    }
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.Message);
            }
        }

        private async Task ShowProperties(string id)
        {
            var properties = await GetProperties("fr", id);
            Properties = (await GetLabels("en", properties.ToArray())).Select(l => new Property(l.id, l.label));
        }

        private async Task ShowInstanceOf(string id)
        {
            var propertiesTask = GetProperties("fr", id);
            var propertiesLabelFrTask = GetLabels("en", (await propertiesTask).ToArray());
            var propertiesLabelFr = await propertiesLabelFrTask;
            var ids = await GetInstanceOfEntities(id);
            var instancesLabelsFrTask = GetLabels("fr", ids.ToArray());
            InstanceOfItems = (await instancesLabelsFrTask).Select(i => new InstanceOfItem(i.id, i.label));
        }

        private async Task<IEnumerable<string>> GetProperties(string lang, params string[] ids)
        {
            List<string> propertyList = new List<string>();
            List<(string name, string value)> labelsList = new List<(string name, string value)>();
            string id = string.Join("|", ids);
            try
            {
                //SelectedSearchItem = null;
                using (HttpClient httpClient = new HttpClient())
                {
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,
                        $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={id}&language=fr&format=json&props=claims"))
                    {
                        var response = await httpClient.SendAsync(request);
                        string allResponse = await response.Content.ReadAsStringAsync();
                        JObject rootResponseJson = Newtonsoft.Json.Linq.JObject.Parse(allResponse);
                        var entities = rootResponseJson["entities"];
                        foreach (JProperty entity in entities)
                        {
                            var qName = entity.Name;
                            var claims = entity.Value["claims"];
                            foreach (JProperty prop in claims)
                            {
                                propertyList.Add(prop.Name);

                                var mainsnak = prop.Value[0]["mainsnak"];
                                var datavalue = mainsnak["datavalue"];
                                var type = datavalue["type"];
                                var theValue = datavalue["value"];
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }

            return propertyList;
        }

        private Task<IEnumerable<string>> GetLabels(string lang, string id)
        {
            return GetLabels(lang, id);
        }
        private async Task<IEnumerable<(string id, string label)>> GetLabels(string lang, params string[] ids)
        {
            List<(string name, string value)> labelsList = new List<(string name, string value)>();
            string id = string.Join("|", ids.Take(50));
            try
            {
                //SelectedSearchItem = null;
                using (HttpClient httpClient = new HttpClient())
                {
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,
                        $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={id}&language=fr&format=json"))
                    {
                        var response = await httpClient.SendAsync(request);
                        string allResponse = await response.Content.ReadAsStringAsync();
                        JObject rootResponseJson = Newtonsoft.Json.Linq.JObject.Parse(allResponse);
                        var entities = rootResponseJson["entities"];
                        if (entities == null)
                            Debug.WriteLine("");
                        foreach (var entity in entities)
                        {
                            var labels = entity.Children()["labels"];
                            var langLabel = labels[lang];
                            var langValue = langLabel["value"];
                            labelsList.Add(((entity as JProperty).Name, langValue.First().Value<string>()));
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.Message);
            }

            return labelsList;
        }

        public async Task<List<string>> GetInstanceOfEntities(string id)
        {
            List<string> instancesIds = new List<string>();
            try
            {
                //SelectedSearchItem = null;
                using (HttpClient httpClient = new HttpClient())
                {
                    using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get,
                        $"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={id}&language=fr&format=json"))
                    {
                        var response = await httpClient.SendAsync(request);
                        string allResponse = await response.Content.ReadAsStringAsync();
                        JObject rootResponseJson = Newtonsoft.Json.Linq.JObject.Parse(allResponse);
                        var entity = rootResponseJson["entities"];
                        var entityClaim = entity[id]["claims"];
                        var instanceOf = entityClaim["P31"];
                        if (instanceOf != null)
                        {
                            foreach (var instance in instanceOf)
                            {
                                var mainsnak = instance["mainsnak"];
                                var datavalue = mainsnak["datavalue"];
                                var type = datavalue["type"];
                                var theValue = datavalue["value"];
                                if (type.Type == JTokenType.String && type.Value<string>() == "wikibase-entityid")
                                {
                                    instancesIds.Add(theValue["id"].Value<string>());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Debug.WriteLine(exc.Message);
            }
            return instancesIds;
        }

        private void BuildModel()
        {
            Model modelGeneration = new Model(SelectedInstanceOfItem, Properties);
            string modelContent = modelGeneration.TransformText();
            System.IO.File.WriteAllText("../../Output.cs", modelContent);
        }
    }
}
