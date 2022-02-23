using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace UserInterfaceDesign
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            Console.SetOut(new ControlWriter(textBox2));
            EnumerateAccounts();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form1.ActiveForm.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ExtractAttachments();
        }

        public class ControlWriter : TextWriter
        {
            private Control textbox;
            public ControlWriter(Control textbox)
            {
                this.textbox = textbox;
            }

            public override void Write(char value)
            {
                textbox.Text += value;
            }

            public override void Write(string value)
            {
                textbox.Text += value;
            }

            public override Encoding Encoding
            {
                get { return Encoding.ASCII; }
            }
        }

        public void EnumerateFoldersInDefaultStore()
        {
            Outlook.Application Application = new Outlook.Application();
            Outlook.Folder root = Application.Session.DefaultStore.GetRootFolder() as Outlook.Folder;
            EnumerateFolders(root);
        }

        // Uses recursion to enumerate Outlook subfolders.
        public void EnumerateFolders(Outlook.Folder folder)
        {
            Outlook.Folders childFolders = folder.Folders;
            if (childFolders.Count > 0)
            {
                foreach (Outlook.Folder childFolder in childFolders)
                {
                    // We only want Inbox folders - ignore Contacts and others
                    if (childFolder.FolderPath.Contains("Inbox"))
                    {
                        // Write the folder path.
                        Console.WriteLine(childFolder.FolderPath);
                        // Call EnumerateFolders using childFolder, to see if there are any sub-folders within this one
                        EnumerateFolders(childFolder);
                    }
                }
            }
            Console.WriteLine("Checking in " + folder.FolderPath);
            IterateMessages(folder);
        }

        public void IterateMessages(Outlook.Folder folder)
        {
            string bPath = SelectedFilePath();
            int totalfilesize = 0;

            // attachment extensions to save.
            List<string> extensionsList = ExtensionsList();

            // Iterate through all items ("messages") in a folder
            var fi = folder.Items;
            if (fi != null)
            {

                try
                {
                    foreach (Object item in fi)
                    {
                        Outlook.MailItem mi = (Outlook.MailItem)item;
                        var attachments = mi.Attachments;
                        if (attachments.Count != 0)
                        {

                            // Create a directory to store the attachment 
                            if (!Directory.Exists(bPath + folder.FolderPath))
                            {
                                Directory.CreateDirectory(bPath + folder.FolderPath);
                            }

                            for (int i = 1; i <= mi.Attachments.Count; i++)
                            {
                                var fn = mi.Attachments[i].FileName.ToLower();
                                //check wither any of the strings in the extensionsArray are contained within the filename
                                if (extensionsList.Any(fn.Contains))
                                {

                                    // Create a further sub-folder for the sender
                                    if (!Directory.Exists(bPath + folder.FolderPath + @"\" + mi.Sender.Address))
                                    {
                                        Directory.CreateDirectory(bPath + folder.FolderPath + @"\" + mi.Sender.Address);
                                    }
                                    totalfilesize = totalfilesize + mi.Attachments[i].Size;
                                    if (!File.Exists(bPath + folder.FolderPath + @"\" + mi.Sender.Address + @"\" + mi.Attachments[i].FileName))
                                    {
                                        Console.WriteLine("Saving " + mi.Attachments[i].FileName);
                                        mi.Attachments[i].SaveAsFile(bPath + folder.FolderPath + @"\" + mi.Sender.Address + @"\" + mi.Attachments[i].FileName);
                                        //mi.Attachments[i].Delete();
                                    }
                                    else
                                    {
                                        Console.WriteLine("Already saved " + mi.Attachments[i].FileName);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine("An error occurred: '{0}'", e);
                }
            }
        }

        // Retrieves the email address for a given account object
        public static string EnumerateAccountEmailAddress(Outlook.Account account)
        {
            try
            {
                if (string.IsNullOrEmpty(account.SmtpAddress) || string.IsNullOrEmpty(account.UserName))
                {
                    Outlook.AddressEntry oAE = account.CurrentUser.AddressEntry as Outlook.AddressEntry;
                    if (oAE.Type == "EX")
                    {
                        Outlook.ExchangeUser oEU = oAE.GetExchangeUser() as Outlook.ExchangeUser;
                        return oEU.PrimarySmtpAddress;
                    }
                    else
                    {
                        return oAE.Address;
                    }
                }
                else
                {
                    return account.SmtpAddress;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        static void EnumerateAccounts()
        {
            Console.WriteLine("Created by: James Reeves");
            Console.WriteLine();
            int id;
            Outlook.Application Application = new Outlook.Application();
            Outlook.Accounts accounts = Application.Session.Accounts;

            id = 1;
            foreach (Outlook.Account account in accounts)
            {
                Console.WriteLine("Run: " + EnumerateAccountEmailAddress(account));
                id++;
            }
            Console.WriteLine("Quit: Quit Application");
            Console.WriteLine();
        }

        public void ExtractAttachments()
        {
            string response = "";
            Outlook.Application Application = new Outlook.Application();
            Outlook.Accounts accounts = Application.Session.Accounts;

            response = UsersInput();

            if (response == "Q")
            {
                Console.WriteLine("Quitting...");
                Form1.ActiveForm.Close();
            }
            if (response != "")
            {

                Console.WriteLine("Processing: " + accounts[Int32.Parse(response.Trim())].DisplayName);

                Outlook.Folder selectedFolder = Application.Session.DefaultStore.GetRootFolder() as Outlook.Folder;
                selectedFolder = GetFolder(@"\\" + accounts[Int32.Parse(response)].DisplayName);
                EnumerateFolders(selectedFolder);
                MessageBox.Show("Job Successfully Completed!" + "\n" + "Your files have been saved here:" +
                    "\n" + SelectedFilePath());
            }
            else
            {
                Console.WriteLine("Invalid Account Selected");
            }

        }

        public string UsersInput()
        {
            string userInput = "";

            if (comboBox1.Text == "Quit")
            {
                userInput = "Q";
            }
            if (comboBox1.Text == "Run")
            {
                userInput = "1";
            }
            return userInput;
        }

        // Returns Folder object based on folder path
        static Outlook.Folder GetFolder(string folderPath)
        {
            Outlook.Folder folder;
            string backslash = @"\";
            try
            {
                if (folderPath.StartsWith(@"\\"))
                {
                    folderPath = folderPath.Remove(0, 2);
                }
                String[] folders = folderPath.Split(backslash.ToCharArray());
                Outlook.Application Application = new Outlook.Application();
                folder = Application.Session.Folders[folders[0]] as Outlook.Folder;
                if (folder != null)
                {
                    for (int i = 1; i <= folders.GetUpperBound(0); i++)
                    {
                        Outlook.Folders subFolders = folder.Folders;
                        folder = subFolders[folders[i]] as Outlook.Folder;
                        if (folder == null)
                        {
                            return null;
                        }
                    }
                }
                return folder;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public void button4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = true;
            // Show the FolderBrowserDialog.  
            DialogResult result = folderDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox6.Text = folderDlg.SelectedPath;
                Environment.SpecialFolder root = folderDlg.RootFolder;
            }
        }

        public string SelectedFilePath()
        {
            string fPath = textBox6.Text;
            return fPath;
        }
        

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            foreach (string s in checkedListBox1.CheckedItems)
                listBox1.Items.Add(s);

            MessageBox.Show("Extension list has been populated. Proceed to next step.");
        }

        public List<string> ExtensionsList()
        {
            List<string> eList = new List<string>();
            foreach (string s in listBox1.Items)
                eList.Add(s);
            return eList;
        }

        private void linkLabel2_Click(object sender, EventArgs e)
        {
            try
            {
                SendEmail();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open link that was clicked.");
            }
        }

        private void SendEmail()
        {
            linkLabel2.LinkVisited = true;

            System.Diagnostics.Process.Start("mailto:" + linkLabel2.Text);
        }

    }
}