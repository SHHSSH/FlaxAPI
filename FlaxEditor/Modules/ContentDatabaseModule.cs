////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) 2012-2017 Flax Engine. All rights reserved.
////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using FlaxEditor.Content;
using FlaxEngine;
using FlaxEngine.Assertions;

namespace FlaxEditor.Modules
{
    /// <summary>
    /// Manages assets database and searches for workspace directory changes.
    /// </summary>
    /// <seealso cref="FlaxEditor.Modules.EditorModule" />
    public sealed class ContentDatabaseModule : EditorModule
    {
        private bool _enableEvents;
        private bool _isDuringFastSetup;
        private int _itemsCreated;
        private int _itemsDeleted;
        private readonly Queue<MainContentTreeNode> _dirtyNodes = new Queue<MainContentTreeNode>();

        /// <summary>
        /// The project content directory.
        /// </summary>
        public MainContentTreeNode ProjectContent { get; private set; }

        /// <summary>
        /// The project source code directory.
        /// </summary>
        public MainContentTreeNode ProjectSource { get; private set; }

        /// <summary>
        /// The engine private content directory.
        /// </summary>
        public MainContentTreeNode EnginePrivate { get; private set; }

        /// <summary>
        /// The editor private content directory.
        /// </summary>
        public MainContentTreeNode EditorPrivate { get; private set; }

        /// <summary>
        /// The list with all content items proxy objects.
        /// </summary>
        public readonly List<ContentProxy> Proxy = new List<ContentProxy>(32);

        /// <summary>
        /// Occurs when new items is added to the workspace content database.
        /// </summary>
        public event Action<ContentItem> OnItemAdded;

        /// <summary>
        /// Occurs when new items is removed from the workspace content database.
        /// </summary>
        public event Action<ContentItem> OnItemRemoved;

        /// <summary>
        /// Occurs when workspace has been modified.
        /// </summary>
        public event Action OnWorkspaceModified;

        /// <summary>
        /// Gets the amount of created items.
        /// </summary>
        /// <value>
        /// The items created.
        /// </value>
        public int ItemsCreated => _itemsCreated;

        /// <summary>
        /// Gets the amount of deleted items.
        /// </summary>
        /// <value>
        /// The items deleted.
        /// </value>
        public int ItemsDeleted => _itemsDeleted;

        internal ContentDatabaseModule(Editor editor)
            : base(editor)
        {
            // Init content database after UI module
            InitOrder = -80;
        }

        /// <summary>
        /// Gets the proxy object for the given content item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Content proxy for that item or null if cannot find.</returns>
        public ContentProxy GetProxy(ContentItem item)
        {
            if (item != null)
            {
                for (int i = 0; i < Proxy.Count; i++)
                {
                    if (Proxy[i].IsProxyFor(item))
                    {
                        return Proxy[i];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the proxy object for the given asset type id.
        /// </summary>
        /// <param name="typeId">The asset type id.</param>
        /// <param name="path">The asset path.</param>
        /// <returns>Asset proxy or null if cannot find.</returns>
        public AssetProxy GetAssetProxy(int typeId, string path)
        {
            for (int i = 0; i < Proxy.Count; i++)
            {
                if (Proxy[i] is AssetProxy proxy && proxy.AcceptsAsset(typeId, path))
                {
                    return proxy;
                }
            }

            return null;
        }

        /// <summary>
        /// Refreshes the given item folder. Tries to find new content items and remove not existing ones.
        /// </summary>
        /// <param name="item">Folder to refresh</param>
        /// <param name="checkSubDirs">True if search for changes inside a subdirectories, otherwise only top-most folder will be updated</param>
        public void RefreshFolder(ContentItem item, bool checkSubDirs)
        {
            // Peek folder to refresh
            ContentFolder folder = item.IsFolder ? item as ContentFolder : item.ParentFolder;
            if (folder == null)
                return;

            // Update
            loadFolder(folder.Node, checkSubDirs);
        }

        /// <summary>
        /// Tries to find item at the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Found item or null if cannot find it.</returns>
        public ContentItem Find(string path)
        {
            Assert.IsFalse(_isDuringFastSetup);

            // TODO: if it's a bottleneck try to optimize searching by spliting path

            var result = ProjectContent.Folder.Find(path);
            if (result != null)
                return result;
            result = ProjectSource.Folder.Find(path);
            if (result != null)
                return result;
            result = EnginePrivate.Folder.Find(path);
            if (result != null)
                return result;
            result = EditorPrivate.Folder.Find(path);

            return result;
        }

        /// <summary>
        /// Tries to find item with the specified ID.
        /// </summary>
        /// <param name="id">The item ID.</param>
        /// <returns>Found item or null if cannot find it.</returns>
        public ContentItem Find(Guid id)
        {
            Assert.IsFalse(_isDuringFastSetup);

            if (id == Guid.Empty)
                return null;

            // TODO: if it's a bottleneck try to optimize searching by caching items IDs

            var result = ProjectContent.Folder.Find(id);
            if (result != null)
                return result;
            result = ProjectSource.Folder.Find(id);
            if (result != null)
                return result;
            result = EnginePrivate.Folder.Find(id);
            if (result != null)
                return result;
            result = EditorPrivate.Folder.Find(id);

            return result;
        }

        /// <summary>
        /// Tries to find script item at the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Found script or null if cannot find it.</returns>
        public ScriptItem FindScript(string path)
        {
            return ProjectSource.Folder.Find(path) as ScriptItem;
        }

        /// <summary>
        /// Tries to find script item with the specified ID.
        /// </summary>
        /// <param name="id">The item ID.</param>
        /// <returns>Found script or null if cannot find it.</returns>
        public ScriptItem FindScript(Guid id)
        {
            if (id == Guid.Empty)
                return null;

            return ProjectSource.Folder.Find(id) as ScriptItem;
        }

        /// <summary>
        /// Tries to find script item with the specified name.
        /// </summary>
        /// <param name="scriptName">The name of the script.</param>
        /// <returns>Found script or null if cannot find it.</returns>
        public ScriptItem FindScriptWitScriptName(string scriptName)
        {
            return ProjectSource.Folder.FindScriptWitScriptName(scriptName);
        }

        /// <summary>
        /// Tries to find script item that is used by the specified script object.
        /// </summary>
        /// <param name="script">The instance of the script.</param>
        /// <returns>Found script or null if cannot find it.</returns>
        public ScriptItem FindScriptWitScriptName(Script script)
        {
            if (script)
            {
                var className = script.GetType().Name;
                var scriptName = ScriptItem.CreateScriptName(className);
                return FindScriptWitScriptName(scriptName);
            }

            return null;
        }

        /// <summary>
        /// Deletes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Delete(ContentItem item)
        {
            if (item == null)
                throw new ArgumentNullException();

            // Fire events
            if (_enableEvents)
                OnItemRemoved?.Invoke(item);
            item.OnDelete();
            _itemsDeleted++;

            var path = item.Path;

            // Special case for folders
            if (item is ContentFolder folder)
            {
                // TODO: maybe dont' remove folders reqursive but at once?

                // Delete all children
                while (folder.Children.Count > 0)
                {
                    Delete(folder.Children[0]);
                }

                // Remove directory
                if (Directory.Exists(path))
                    Directory.Delete(path);

                // Unlink from the parent
                item.ParentFolder = null;

                // Delete tree node
                folder.Node.Dispose();
            }
            else
            {
                // Check if it's an asset
                if (item.IsAsset)
                {
                    // Delete asset by using content pool
                    throw new NotImplementedException("FlaxEngine.Content.Delete assets");
                    //FlaxEngine.Content.Delete(path);
                }
                else
                {
                    // Delete file
                    if (File.Exists(path))
                        File.Delete(path);
                }

                // Unlink from the parent
                item.ParentFolder = null;

                // Delete item
                item.Dispose();
            }
        }

        private void loadFolder(ContentTreeNode node, bool checkSubDirs)
        {
            // Temporary data
            var folder = node.Folder;
            var path = folder.Path;

            // Check for missing files/folders (skip it during fast tree setup)
            if (!_isDuringFastSetup)
            {
                for (int i = 0; i < folder.Children.Count; i++)
                {
                    var child = folder.Children[i];
                    if (!child.Exists)
                    {
                        // Send info
                        Debug.Log(string.Format($"Content item \'{child.Path}\' has been removed"));
                        
                        // Destroy it
                        Delete(child);

                        i--;
                    }
                }
            }

            // Find elements (use separate path for scripts and assets - perf stuff)
            // TODO: we could make it more modular
            if (node.CanHaveAssets)
            {
                LoadAssets(node, path);
            }
            if (node.CanHaveScripts)
            {
                LoadScripts(node, path);
            }

            // Get child directories
            var childFolders = System.IO.Directory.GetDirectories(path);
            
            // Load child folders
            bool sortChildren = false;
            for (int i = 0; i < childFolders.Length; i++)
            {
                var childPath = childFolders[i];

                // Check if node already has that element (skip during init when we want to walk project dir very fast)
                ContentFolder childFolderNode = _isDuringFastSetup ? null : node.Folder.FindChild(childPath) as ContentFolder;
                if (childFolderNode == null)
                {
                    // Create node
                    ContentTreeNode n = new ContentTreeNode(node, childPath);
                    if (!_isDuringFastSetup)
                        sortChildren = true;
                    
                    // Load child folder
                    loadFolder(n, true);

                    // Fire event
                    if (_enableEvents)
                        OnItemAdded?.Invoke(n.Folder);
                    _itemsCreated++;
                }
                else if (checkSubDirs)
                {
                    // Update child folder
                    loadFolder(childFolderNode.Node, true);
                }
            }
            if (sortChildren)
                node.SortChildren();
        }

        private void LoadScripts(ContentTreeNode parent, string directory)
        {
            // Find files
            var files = Directory.GetFiles(directory, ScriptProxy.ExtensionFiler, SearchOption.TopDirectoryOnly);

            // Add them
            for (int i = 0; i < files.Length; i++)
            {
                var path = files[i];

                // Check if node already has that element (skip during init when we want to walk project dir very fast)
                if (_isDuringFastSetup || !parent.Folder.ContaisnChild(path))
                {
                    // Create item object
                    var item = new ScriptItem(path);

                    // Link
                    item.ParentFolder = parent.Folder;

                    // Fire event
                    if (_enableEvents)
                        OnItemAdded?.Invoke(item);
                    _itemsCreated++;
                }
            }
        }

        private void LoadAssets(ContentTreeNode parent, string directory)
        {
            // Find files
            var files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);

            // Add them
            for (int i = 0; i < files.Length; i++)
            {
                var path = files[i];

                // Check if node already has that element (skip during init when we want to walk project dir very fast)
                if (_isDuringFastSetup || !parent.Folder.ContaisnChild(path))
                {
                    // It can be any type of asset: binary, text, cooked, package, etc.
                    // The best idea is to just ask Flax.
                    // Flax isn't John Snow. Flax knows something :)
                    // Also Flax Content Layer is using smart caching so this query gonna be fast.

                    int typeId;
                    Guid id;
                    if (FlaxEngine.Content.GetAssetInfo(path, out typeId, out id))
                    {
                        var proxy = GetAssetProxy(typeId, path);
                        var item = proxy?.ConstructItem(path, typeId, ref id);
                        if (item != null)
                        {
                            // Link
                            item.ParentFolder = parent.Folder;

                            // Fire event
                            if (_enableEvents)
                                OnItemAdded?.Invoke(item);
                            _itemsCreated++;
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void OnInit()
        {
            // Setup content proxies
            Proxy.Add(new TextureProxy());
            Proxy.Add(new ModelProxy());
            Proxy.Add(new MaterialProxy());
            Proxy.Add(new MaterialInstanceProxy());
            Proxy.Add(new SpriteAtlasProxy());
            Proxy.Add(new CubeTextureProxy());
            Proxy.Add(new PreviewsCacheProxy());
            Proxy.Add(new FontProxy());
            Proxy.Add(new ScriptProxy());
            Proxy.Add(new SceneProxy());
            Proxy.Add(new IESProfileProxy());

            // Create content folders nodes
            ProjectContent = new MainContentTreeNode(ContentFolderType.Content, Globals.ContentFolder);
            ProjectSource = new MainContentTreeNode(ContentFolderType.Source, Globals.SourceFolder);
            EnginePrivate = new MainContentTreeNode(ContentFolderType.Editor, Globals.EngineFolder);
            EditorPrivate = new MainContentTreeNode(ContentFolderType.Engine, Globals.EditorFolder);

            // Load all folders
            // TODO: we should create async task for gathering content and whole workspace contents if it takes too long
            // TODO: create progress bar in content window and after end we should enable events and update it
            loadFolder(ProjectContent, true);
            loadFolder(ProjectSource, true);
            loadFolder(EnginePrivate, true);
            loadFolder(EditorPrivate, true);
            _isDuringFastSetup = false;

            // Enable events
            _enableEvents = true;

            Debug.Log("Project database created. Items count: " + _itemsCreated);
        }

        internal void OnDirectoryEvent(MainContentTreeNode node, FileSystemEventArgs e)
        {
            // Ensure to be ready for external events
            if (_isDuringFastSetup)
                return;

            // TODO: maybe we could make it faster! since we have a path so it would be easy to just create or delete given file
            // TODO: but remember about subdirectories!

            // Switch type
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Deleted:
                {
                    // We want to enqueue dir modification events for better stability
                    if (!_dirtyNodes.Contains(node))
                        _dirtyNodes.Enqueue(node);

                    break;
                }

                default: break;
            }
        }

        /// <inheritdoc />
        public override void OnUpdate()
        {
            while (_dirtyNodes.Count > 0)
            {
                // Get node
                var node = _dirtyNodes.Dequeue();

                // Refresh
                loadFolder(node, true);

                // Fire event
                if (_enableEvents)
                    OnWorkspaceModified?.Invoke();
            }
        }

        /// <inheritdoc />
        public override void OnExit()
        {
            // Disable events
            _enableEvents = false;

            // Cleanup
            Proxy.ForEach(x => x.Dispose());
            ProjectContent.Dispose();
            ProjectSource.Dispose();
            EnginePrivate.Dispose();
            EditorPrivate.Dispose();
            ProjectContent = null;
            ProjectSource = null;
            EnginePrivate = null;
            EditorPrivate = null;
            Proxy.Clear();
        }
    }
}
