using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GitCommands;
using ResourceManager.Translation;

namespace GitUI
{
    public partial class FormGerritPublish : FormGerritBase
    {
        private string _currentBranchRemote;

        #region Translation
        private readonly TranslationString _downloadGerritChangeCaption = new TranslationString("Download Gerrit Change");

        private readonly TranslationString _publishCaption = new TranslationString("Publish change");

        private readonly TranslationString _selectRemote = new TranslationString("Please select a remote repository");
        private readonly TranslationString _selectBranch = new TranslationString("Please enter a branch");
        #endregion

        public FormGerritPublish()
        {
            InitializeComponent();
            Translate();
        }

        public void PushAndShowDialogWhenFailed(IWin32Window owner)
        {
            if (!DownloadChange(owner))
                ShowDialog(owner);
        }

        public void PushAndShowDialogWhenFailed()
        {
            PushAndShowDialogWhenFailed(null);
        }

        private void PublishClick(object sender, EventArgs e)
        {
            if (DownloadChange(this))
                Close();
        }

        private bool DownloadChange(IWin32Window owner)
        {
            string branch = _NO_TRANSLATE_Branch.Text.Trim();

            if (string.IsNullOrEmpty(_NO_TRANSLATE_Remotes.Text))
            {
                MessageBox.Show(owner, _selectRemote.Text);
                return false;
            }
            if (string.IsNullOrEmpty(branch))
            {
                MessageBox.Show(owner, _selectBranch.Text);
                return false;
            }

            StartAgent(owner, _NO_TRANSLATE_Remotes.Text);

            string topic = _NO_TRANSLATE_Topic.Text.Trim();

            if (string.IsNullOrEmpty(topic))
                topic = GetTopic(branch);

            string targetRef = PublishDraft.Checked ? "drafts" : "publish";

            string pushCmd = GitCommandHelpers.PushCmd(
                _NO_TRANSLATE_Remotes.Text,
                "refs/" + targetRef + "/" + branch + "/" + topic,
                false
            );

            var form = new FormRemoteProcess(pushCmd)
            {
                Remote = _NO_TRANSLATE_Remotes.Text,
                Text = _publishCaption.Text
            };

            form.ShowDialog(owner);

            if (!form.ErrorOccurred())
            {
                bool hadNewChanges = false;
                string change = null;

                foreach (string line in form.OutputString.ToString().Split('\n'))
                {
                    if (hadNewChanges)
                    {
                        change = line;
                        const string remotePrefix = "remote:";

                        if (change.StartsWith(remotePrefix))
                            change = change.Substring(remotePrefix.Length);

                        int escapePos = change.LastIndexOf((char)27);
                        if (escapePos != -1)
                            change = change.Substring(0, escapePos);

                        change = change.Trim();

                        int spacePos = change.IndexOf(' ');
                        if (spacePos != -1)
                            change = change.Substring(0, spacePos);

                        break;
                    }
                    else if (line.Contains("New Changes"))
                    {
                        hadNewChanges = true;
                    }
                }

                if (change != null)
                    FormGerritChangeSubmitted.ShowSubmitted(owner, change);
            }

            return true;
        }

        private string GetTopic(string targetBranch)
        {
            string branchName = GetBranchName(targetBranch);

            string[] branchParts = branchName.Split('/');

            if (branchParts.Length >= 3 && branchParts[0] == "review")
                return String.Join("/", branchParts.Skip(2));

            return branchName;
        }

        private string GetBranchName(string targetBranch)
        {
            string branch = GitCommands.Settings.Module.GetSelectedBranch();

            if (branch.StartsWith("(no"))
                return targetBranch;

            return branch;
        }

        private void FormGerritPublishLoad(object sender, EventArgs e)
        {
            RestorePosition("public-gerrit-change");

            _NO_TRANSLATE_Remotes.DataSource = GitCommands.Settings.Module.GetRemotes();

            _currentBranchRemote = Settings.DefaultRemote;

            IList<string> remotes = (IList<string>)_NO_TRANSLATE_Remotes.DataSource;
            int i = remotes.IndexOf(_currentBranchRemote);
            _NO_TRANSLATE_Remotes.SelectedIndex = i >= 0 ? i : 0;

            _NO_TRANSLATE_Branch.Text = Settings.DefaultBranch;

            _NO_TRANSLATE_Branch.Select();

            Text = string.Concat(_downloadGerritChangeCaption.Text, " (", GitCommands.Settings.WorkingDir, ")");
        }

        private void AddRemoteClick(object sender, EventArgs e)
        {
            GitUICommands.Instance.StartRemotesDialog(this);
            _NO_TRANSLATE_Remotes.DataSource = GitCommands.Settings.Module.GetRemotes();
        }

        private void FormGerritPublish_FormClosing(object sender, FormClosingEventArgs e)
        {
            SavePosition("public-gerrit-change");
        }
    }
}