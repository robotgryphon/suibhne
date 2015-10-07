using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using Nini.Config;

namespace Ostenvighx.Suibhne.Gui.Windows {
    /// <summary>
    /// Interaction logic for LocationEditor.xaml
    /// </summary>
    public partial class LocationEditor : Window {

        // Location information
        private Guid id;
        private Location location;
        private String networkType;
        private Dictionary<String, FrameworkElement> fields;

        private LocationEditor() {
            InitializeComponent();
        }

        public LocationEditor(Guid id, bool newLocation = false)
            : this() {
            this.id = id;
            this.location = LocationManager.GetLocationInfo(id);
            this.fields = new Dictionary<string, FrameworkElement>();

            this.Title += " - " + location.Name;

            LoadNetworkInformation();
            LoadConfigFields();
            if (!newLocation)
                LoadExistingData();
        }

        private void LoadNetworkInformation() {
            String path = "";
            if (location.Type != Reference.LocationType.Network) {
                if (location.Parent != Guid.Empty) {
                    path = Core.ConfigDirectory + "/Networks/" + location.Parent + "/network.ini";
                } else {
                    MessageBox.Show("Error: Somehow, this location does not have a parent set, " +
                    "and we can't figure out what network type it is. As such, configuration fields cannot be loaded." +
                    " Please check the location config file for corruption and edit or regenerate it as necessary. Sorry!");
                    this.Close();
                }

            } else {
                path = Core.ConfigDirectory + "/Networks/" + id + "/network.ini";
            }

            try {
                IniConfigSource config = new IniConfigSource(path);
                config.CaseSensitive = false;
                if (config == null || config.Configs["Network"] == null) {
                    MessageBox.Show("Error: The network configuration file appears to be missing its network type information. Please check or regenerate that file as needed." + Environment.NewLine
                        + "It is located at: " + path);
                    this.Close();
                }

                this.networkType = config.Configs["Network"].GetString("type");
            }

            catch (Exception) {
                MessageBox.Show("There was an error getting the network information. Please verify the network (not the location!) configuration files are properly generated and untouched.");
                this.Close();
            }
        }

        private void LoadConfigFields() {
            XmlDocument xml = new XmlDocument();
            try {
                String netConfigTemplateFilename = Core.ConfigDirectory + @"Connectors\" + networkType + "\\Config.xml";
                String stackText = File.ReadAllText(netConfigTemplateFilename);
                xml.LoadXml(stackText);
            }

            catch (Exception) {

                MessageBox.Show("There was an error loading the configuration fields. Looks like the xml document containing the network config structure had a problem loading.");
                this.Close();
            }

            switch (location.Type) {
                case Reference.LocationType.Network:
                    // Network editing
                    XmlNode networkRoot = xml.ChildNodes[1].SelectSingleNode("Network");
                    foreach(XmlNode node in networkRoot.ChildNodes)
                        ParseConfigStructure(node);
                    break;

                case Reference.LocationType.Public:
                case Reference.LocationType.Private:
                    // Location editing
                    XmlNode locationRoot = xml.ChildNodes[1].SelectSingleNode("Location");
                    foreach(XmlNode node in locationRoot.ChildNodes)
                        ParseConfigStructure(node);
                    break;

                case Reference.LocationType.Unknown:
                    MessageBox.Show("This type of location is not supported by the GUI editor. Please refer to the connector's documentation (if any) for support.");
                    return;
            }
        }

        private void ParseConfigStructure(XmlNode n) {
            switch (n.Name.ToLower()) {
                case "field":
                    WrapPanel fieldPanel = new WrapPanel();
                    if (n.Attributes["Type"] == null) return;

                    String sectionName = n.ParentNode.Attributes["Name"].Value;

                    if (n.Attributes["Label"] != null) {
                        Label fieldLabel = new Label();
                        fieldLabel.Content = n.Attributes["Label"];
                        fieldLabel.Width = 120;
                        fieldPanel.Children.Add(fieldLabel);
                    }

                    switch (n.Attributes["Type"].Value.ToLower()) {
                        case "text":
                            TextBox tb = new TextBox();
                            tb.Height = 20;
                            tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                            tb.MinWidth = 400;
                            tb.Name = n.Attributes["Key"].Value;
                            fieldPanel.Children.Add(tb);
                            fields.Add(tb.Name, tb);
                            break;

                        case "checkbox":
                            CheckBox cb = new CheckBox();
                            cb.Margin = new Thickness(0, 5, 0, 5);
                            cb.Name = n.Attributes["Key"].Value;
                            fieldPanel.Children.Add(cb);
                            fields.Add(cb.Name, cb);
                            break;

                        case "password":
                            PasswordBox p = new PasswordBox();
                            p.Height = 20;
                            p.HorizontalAlignment = HorizontalAlignment.Stretch;
                            p.MinWidth = 400;
                            p.Name = n.Attributes["Key"].Value;
                            fieldPanel.Children.Add(p);
                            fields.Add(p.Name, p);
                            break;

                        case "select":
                            ComboBox select = new ComboBox();
                            select.Width = 400;
                            select.Name = n.Attributes["Key"].Value;

                            foreach (XmlNode childNode in n.ChildNodes) {
                                ComboBoxItem cbi = new ComboBoxItem();
                                cbi.Content = childNode.InnerText;

                                select.Items.Add(cbi);
                            }

                            fieldPanel.Children.Add(select);
                            fields.Add(select.Name, select);
                            break;

                        default:
                            // Unsupported type
                            break;

                    }
                    this.locationConfigPanel.Children.Add(fieldPanel);
                    break;

                case "section":
                    Label l = new Label();
                    l.Content = n.Attributes["Name"].Value;
                    l.FontSize = 20;
                    l.Margin = new Thickness(0, 20, 40, 6);
                    l.BorderThickness = new Thickness(0, 0, 0, 1);
                    l.BorderBrush = new SolidColorBrush(Colors.SlateBlue);
                    this.locationConfigPanel.Children.Add(l);

                    foreach (XmlNode childNode in n.ChildNodes) {
                        ParseConfigStructure(childNode);
                    }
                    break;

                case "text":
                    Label b = new Label();
                    if (n.InnerText != "")
                        b.Content = n.InnerText;

                    if (n.Attributes["Bold"] != null)
                        b.SetValue(Label.FontWeightProperty, FontWeights.Bold);

                    this.locationConfigPanel.Children.Add(b);
                    break;

                default:
                    Console.WriteLine("Unsupported node type: " + n.Name);
                    break;
            }
        }

        private void LoadExistingData() {

            String configFilePath = "";

            if(location.Parent == Guid.Empty)
                configFilePath = Core.ConfigDirectory + "/Networks/" + id + "/network.ini";
            else
                configFilePath = Core.ConfigDirectory + "/Networks/" + location.Parent + "/Locations/" + id + "/location.ini";

            try {
                IniConfigSource config = new IniConfigSource(configFilePath);
                XmlDocument xml = new XmlDocument();
                xml.Load(Core.ConfigDirectory + "/Connectors/" + networkType + "/Config.xml");

                foreach(XmlNode node in xml.ChildNodes[1].SelectSingleNode(location.Type == Reference.LocationType.Network ? "Network" : "Location")){
                    LoadDataWithXml(node, config);
                }
            }

            catch (XmlException) {
                MessageBox.Show("There was an error loading the configuration fields. Looks like the xml document containing the network config structure had a problem loading.");
                this.Close();
            }

            catch (Exception e) {
                Core.Log("Exception caught: " + e.Message, LogType.ERROR);
            }

        }

        /// <summary>
        /// Recursively compares a generated config file and the template document to load in all the
        /// existing configuration information.
        /// </summary>
        private void LoadDataWithXml(XmlNode node, IniConfigSource config) {
            switch (node.Name.ToLower()) {
                case "section":
                    foreach(XmlNode childNode in node.ChildNodes)
                        LoadDataWithXml(childNode, config);
                    break;

                case "field":
                    String fieldName = node.Attributes["Key"].Value;
                    String section = node.ParentNode.Attributes["Name"].Value;

                    Core.Log("Loading field: " + fieldName);

                    switch (node.Attributes["Type"].Value.ToLower()) {
                        case "text":
                            ((TextBox)fields[fieldName]).Text = config.Configs[section].GetString(fieldName);
                            break;

                        case "password":
                            ((PasswordBox)fields[fieldName]).Password = config.Configs[section].GetString(fieldName);
                            break;

                        case "checkbox":
                            ((CheckBox)fields[fieldName]).IsChecked = config.Configs[section].GetBoolean(fieldName);
                            break;
                    }

                    break;
            }
        }

        #region Event Handling
        private void SaveDataToDisk_Handler(object sender, RoutedEventArgs e) {

        }

        private void RefreshFromDisk_Handler(object sender, RoutedEventArgs e) {
            LoadExistingData();
        }
        #endregion
    }
}
