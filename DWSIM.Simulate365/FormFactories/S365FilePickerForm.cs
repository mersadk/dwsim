using DWSIM.Interfaces;
using DWSIM.Logging;
using DWSIM.Simulate365.Models;
using DWSIM.Simulate365.Services;
using DWSIM.UI.Web;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace DWSIM.Simulate365.FormFactories
{
    public class S365FilePickerForm : IFilePicker
    {
        private WebUIForm _webUIForm;
        private readonly FilePickerService _filePickerService;

        public string SuggestedDirectory { get; set; }
        public string SuggestedFilename { get; set; }

        private readonly UserService _userService;

        public static bool CollaborationEnabled;


        #region Public events

        public static event EventHandler FileOpenedFromDashboard;

        public static event EventHandler<BeforeShowDialogEventArgs> BeforeShowSaveDialog;

        public static event EventHandler<BeforeShowDialogEventArgs> BeforeShowOpenDialog;
        public event EventHandler AfterUserLoggedIn;

        #endregion

        public S365FilePickerForm()
        {

            _filePickerService = new FilePickerService();
            _filePickerService.S3365DashboardFileOpenStarted += FilePickerService_S3365DashboardFileOpenStarted;
            _filePickerService.S365DashboardSaveFileClicked += FilePickerService_S365DashboardSaveFileClicked;
            _filePickerService.S365DashboardFolderCreated += _filePickerService_S365DashboardFolderCreated;
            _userService = UserService.GetInstance();
            _userService.OnUserLoggedIn += OnUserLoggedInEvent;           
        }


        private void OnUserLoggedInEvent(object sender, EventArgs e)
        {
            if (AfterUserLoggedIn != null)
            {
                AfterUserLoggedIn?.Invoke(this, new EventArgs());
            }
            else if (_webUIForm != null)
            {
                _webUIForm.RealoadPage();
            }
        }

        private void _filePickerService_S365DashboardFolderCreated(object sender, EventArgs e)
        {
            if (_webUIForm != null)
            {
                _webUIForm.RealoadPage();
            }
        }

        private void FilePickerService_S365DashboardSaveFileClicked(object sender, S365DashboardSaveFile e)
        {
            UsubscribeFromEvents();

            // Close window
            _webUIForm.SafeClose();
            _webUIForm = null;
        }
        private void UsubscribeFromEvents()
        {
            _userService.OnUserLoggedIn -= OnUserLoggedInEvent;
            _filePickerService.S3365DashboardFileOpenStarted -= FilePickerService_S3365DashboardFileOpenStarted;
            _filePickerService.S365DashboardSaveFileClicked -= FilePickerService_S365DashboardSaveFileClicked;
            _filePickerService.S365DashboardFolderCreated -= _filePickerService_S365DashboardFolderCreated;
        }

        private void FilePickerService_S3365DashboardFileOpenStarted(object sender, EventArgs e)
        {
            UsubscribeFromEvents();
            // Close window
            _webUIForm.SafeClose();
            _webUIForm = null;
        }

        public void Close()
        {
            UsubscribeFromEvents();
            _webUIForm.SafeClose();
            _webUIForm = null;
        }

        public S365File ShowSaveDialog(List<string> fileFormats = null, bool isSaveAs = false, bool isLeavingCollaborationFile = false)
        {
            // Invoke event handlers
            var eventArgs = new BeforeShowDialogEventArgs();
            BeforeShowSaveDialog?.Invoke(null, eventArgs);
            if (eventArgs.Cancel)
                return null;

            var navigationPath = "filepicker/save";
            var queryParams = new Dictionary<string, string>();
            if (fileFormats != null && fileFormats.Count > 0)
                queryParams.Add("extensions", string.Join("_", fileFormats));

            if (!string.IsNullOrWhiteSpace(SuggestedDirectory))
            {
                // If user has opened collaboration file, and tries to save that file he will get wrong SuggestedDirectory.
                // We could compare OwnerId of opened file with currentUserId, but getting opened file data from here is issue.
                // For now we will just disable setting suggestedDirectory inside save form if collaboration is enabled.
                if (!CollaborationEnabled)
                    queryParams.Add("directory", HttpUtility.UrlEncode(SuggestedDirectory));
            }

            if (isSaveAs)
                queryParams.Add("saveAs", "true");

            if (isLeavingCollaborationFile)
                queryParams.Add("leavingCollaborationFile", "true");

            if (!string.IsNullOrWhiteSpace(SuggestedFilename))
                queryParams.Add("filename", HttpUtility.UrlEncode(SuggestedFilename));

            var initialUrl = $"{navigationPath}";
            if (queryParams.Any())
            {
                initialUrl = initialUrl + string.Join("", queryParams.Select(x =>
                {
                    var param = $"{x.Key}={x.Value}";
                    return queryParams.First().Key == x.Key ? $"?{param}" : $"&{param}";

                }).ToList());
            }

            string title = $"Save {(isSaveAs ? "As" : "")} file to Simulate 365 Dashboard";
            _webUIForm = new WebUIForm(initialUrl, title, true)
            {
                Width = (int)(1300 * DWSIM.GlobalSettings.Settings.DpiScale),
                Height = (int)(800 * DWSIM.GlobalSettings.Settings.DpiScale)
            };

            _webUIForm.SubscribeToInitializationCompleted(Browser_CoreWebView2InitializationCompleted);

            _webUIForm.ShowDialog();

            return _filePickerService.SelectedSaveFile != null ?
                new S365File(null)
                {
                    FileUniqueIdentifier = null,
                    Filename = _filePickerService.SelectedSaveFile.Filename,
                    ParentUniqueIdentifier = _filePickerService.SelectedSaveFile.ParentUniqueIdentifier,
                    FullPath = _filePickerService.SelectedSaveFile.SimulatePath,
                    ConflictAction = _filePickerService.SelectedSaveFile.ConflictAction,
                    OwnerId = UserService.GetInstance().CurrentUser?.Id,
                    IsSharedForCollaboration = _filePickerService.SelectedSaveFile.IsSharedForCollaboration
                } : null;
        }


        public S365File ShowOpenDialog(List<string> fileFormats = null)
        {
            // Invoke event handlers
            var eventArgs = new BeforeShowDialogEventArgs();
            BeforeShowOpenDialog?.Invoke(null, eventArgs);
            if (eventArgs.Cancel)
                return null;

            var navigationPath = "filepicker/open";
            var queryParams = new Dictionary<string, string>();
            if (fileFormats != null && fileFormats.Count > 0)
            {
                queryParams.Add("extensions", string.Join("_", fileFormats));
            }
            if (!string.IsNullOrWhiteSpace(SuggestedDirectory))
            {
                queryParams.Add("directory", HttpUtility.UrlEncode(SuggestedDirectory));
            }

           
            if (CollaborationEnabled)
            {
                queryParams.Add("collaboration", "true");
            }

            var initialUrl = $"{navigationPath}";
            if (queryParams.Any())
            {
                initialUrl = initialUrl + string.Join("", queryParams.Select(x =>
                {
                    var param = $"{x.Key}={x.Value}";
                    return queryParams.First().Key == x.Key ? $"?{param}" : $"&{param}";

                }).ToList());
            }
            string title = "Open file from Simulate 365 Dashboard";
            _webUIForm = new WebUIForm(initialUrl, title, true)
            {
                Width = (int)(1300 * DWSIM.GlobalSettings.Settings.DpiScale),
                Height = (int)(800 * DWSIM.GlobalSettings.Settings.DpiScale)
            };

            _webUIForm.SubscribeToInitializationCompleted(Browser_CoreWebView2InitializationCompleted);

            _webUIForm.ShowDialog();

          
            return _filePickerService.SelectedOpenFile;
        }


        private void Browser_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            var webView = sender as WebView2;
            if (webView == null)
            {
                Logger.LogError("S365FilePickerForm: initialization callback sender is not WebView2.", null);
                return;
            }

            if (!e.IsSuccess)
            {
                Logger.LogError("S365FilePickerForm: WebView2 initialization failed.", e.InitializationException);
                return;
            }

            if (webView.CoreWebView2 == null)
            {
                Logger.LogError("S365FilePickerForm: CoreWebView2 is null after successful initialization.", null);
                return;
            }

            TryAddHostObject(webView, "authService", new AuthService());
            TryAddHostObject(webView, "filePickerService", _filePickerService);
        }

        private void TryAddHostObject(WebView2 webView, string objectName, object hostObject)
        {
            try
            {
                webView.CoreWebView2.AddHostObjectToScript(objectName, hostObject);
            }
            catch (Exception ex)
            {
                Logger.LogError($"S365FilePickerForm: failed to add host object '{objectName}'.", ex);
            }
        }

        #region FilePickerService

        public IVirtualFile ShowOpenDialog(IEnumerable<IFilePickerAllowedType> allowedTypes)
        {
            List<string> fileFormats = null;
            if (allowedTypes != null && allowedTypes.Count() > 0)
            {
                fileFormats = allowedTypes.SelectMany(t => t.AllowedExtensions.Select(e => ReplateLeadingStarDot(e))).Distinct().ToList();
            }

            var file = ShowOpenDialog(fileFormats);

            FileOpenedFromDashboard?.Invoke(this, new EventArgs());
            return file;
        }

        public IVirtualFile ShowSaveDialog(IEnumerable<IFilePickerAllowedType> allowedTypes, bool isSaveAs = false, bool isLeavingCollaborationFile = false)
        {
            List<string> fileFormats = null;
            if (allowedTypes != null && allowedTypes.Count() > 0)
            {
                fileFormats = allowedTypes.SelectMany(t => t.AllowedExtensions.Select(e => ReplateLeadingStarDot(e))).Distinct().ToList();
            }

            var file = ShowSaveDialog(fileFormats, isSaveAs, isLeavingCollaborationFile);
            return file;
        }

        private string ReplateLeadingStarDot(string input)
        {
            return Regex.Replace(input, @"^\*{0,1}\.", "");
        }


        #endregion
    }

    public class BeforeShowDialogEventArgs : EventArgs
    {
        public bool Cancel { get; set; }
    }
}
