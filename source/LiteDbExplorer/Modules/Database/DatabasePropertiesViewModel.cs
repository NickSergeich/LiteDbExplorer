﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Caliburn.Micro;
using Humanizer;
using JetBrains.Annotations;
using LiteDbExplorer.Windows;
using LiteDB;
using LiteDbExplorer.Core;

namespace LiteDbExplorer.Modules.Database
{
    [Export(typeof(IDatabasePropertiesView))]
    public class DatabasePropertiesViewModel : Screen, IDatabasePropertiesView
    {
        private LiteDatabase _database;
        private ushort _userVersion;
        private DatabaseReference _databaseReference;

        public DatabasePropertiesViewModel()
        {
            DisplayName = "Database Properties";
        }

        public void Init(DatabaseReference databaseReference)
        {
            _databaseReference = databaseReference;
            _database = databaseReference.LiteDatabase;

            DisplayName = $"Database Properties - {databaseReference.Name}";

            UserVersion = _database.Engine.UserVersion;

            SetDatabaseInfo();
        }

        public IEnumerable<DisplayInfo> DatabaseInfo { get; private set; }

        public IEnumerable<DisplayInfo> DatabaseFileInfo { get; private set; }

        public ushort UserVersion
        {
            get => _userVersion;
            set
            {
                _userVersion = value;
                HasChanges = true;
            }
        }

        public bool HasChanges { get; private set; }

        public bool CanAcceptButton => HasChanges;

        public void AcceptButton()
        {
            if (_database.Engine.UserVersion != UserVersion)
            {
                _database.Engine.UserVersion = UserVersion;
            }

            TryClose(true);
        }

        public bool CanCancelButton => true;

        public void CancelButton()
        {
            TryClose(false);
        }

        [UsedImplicitly]
        public void ShrinkDatabase()
        {
            var oldSizeInfo = DatabaseFileInfo.FirstOrDefault(p => p.HasTag("FileSize"));

            _database.Shrink();
            SetDatabaseInfo();

            var newSizeInfo = DatabaseFileInfo.FirstOrDefault(p => p.HasTag("FileSize"));

            var message = $"Database {_databaseReference.Name} shrink completed.\n";
            if (oldSizeInfo != null && newSizeInfo != null)
            {
                message += $"From {oldSizeInfo.Content} to {newSizeInfo.Content}";
            }

            MessageBox.Show(message, "Shrink Database", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        [UsedImplicitly]
        public void SetPassword()
        {
            if (InputBoxWindow.ShowDialog("New password, enter empty string to remove password.", "", "", null, out string password) == true)
            {
                _database.Shrink(string.IsNullOrEmpty(password) ? null : password);
                SetDatabaseInfo();
            }
        }

        private void SetDatabaseInfo()
        {
            var engineInfoDocument = _database.Engine.Info();
            var databaseInfo = new List<DisplayInfo>
            {
                new DisplayInfo("Collections:", _databaseReference.Collections.Count),
                new DisplayInfo("Encrypted:", engineInfoDocument["encrypted"].AsBoolean),
                new DisplayInfo("Change ID:", engineInfoDocument["changeID"].RawValue),
                new DisplayInfo("Last page ID:", engineInfoDocument["lastPageID"].RawValue)
            };
            // var engineInfo = engineInfoDocument.Select(p => new DisplayInfo(p.Key, p.Value));
            // databaseInfo.AddRange(engineInfo);
            DatabaseInfo = databaseInfo;

            var fileInfo = new FileInfo(_databaseReference.Location);
            var databaseFileInfo = new List<DisplayInfo>
            {
                new DisplayInfo("File name:", fileInfo.Name),
                new DisplayInfo("Location:", fileInfo.DirectoryName),
                new DisplayInfo("File size:", fileInfo.Length.Bytes().ToString("#.##")) {Tag = "FileSize"},
                new DisplayInfo("Created:", fileInfo.CreationTime),
                new DisplayInfo("Last access:", fileInfo.LastAccessTime),
                new DisplayInfo("Last write:", fileInfo.LastWriteTime)
            };

            DatabaseFileInfo = databaseFileInfo;
        }
    }

    public class DisplayInfo : INotifyPropertyChanged
    {
        public DisplayInfo(string title, object content)
        {
            Title = title;
            Content = content;
        }

        public string Title { get; set; }
        public object Content { get; set; }
        public string Tag { get; set; }

        public bool HasTag(string tag)
        {
            return Tag != null && Tag.Equals(tag, StringComparison.OrdinalIgnoreCase);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [UsedImplicitly]
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}