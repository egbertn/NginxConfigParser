using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace NginxConfigParser
{
    /// <summary>
    ///  Represents an nginx configuration file operation object.
    /// </summary>
    public class NginxConfig
    {
        private readonly Parser _parser;

        private IList<IToken> _tokens = new Collection<IToken>();

        protected NginxConfig(Parser parser)
        {
            _parser = parser;

            Initial();
        }

        /// <summary>
        ///  Create an new
        /// </summary>
        public static NginxConfig Create()
        {
            var parser = new Parser(string.Empty);

            return new NginxConfig(parser);
        }

        /// <summary>
        ///  Load from specific file
        /// </summary>
        /// <param name="fileName">The file path</param>
        /// <returns><see cref="NginxConfig"/></returns>
        public static NginxConfig LoadFrom(string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileName,nameof(fileName));

            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }

            var content = File.ReadAllText(fileName);

            return Load(content);
        }
        /// <summary>
        ///  Load from specific file
        /// </summary>
        /// <param name="fileName">The file path</param>
        /// <returns><see cref="NginxConfig"/></returns>
        public static async Task<NginxConfig> LoadFromAsync(string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileName,nameof(fileName));

            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException(fileName);
            }
            var content = await File.ReadAllTextAsync(fileName);

            return Load(content);
        }

        /// <summary>
        ///  Load from file content
        /// </summary>
        /// <param name="content">The string of file content</param>
        /// <returns><see cref="NginxConfig"/></returns>
        public static NginxConfig Load(string content)
        {
            ArgumentNullException.ThrowIfNull(content, nameof(content));
            return new NginxConfig( new Parser(content));
        }

        private void Initial()
        {
            _parser.Parse();
            _tokens = _parser.GetTokens().ToList();
        }

        /// <summary>
        ///  Get all values
        /// </summary>
        public IEnumerable<IToken> GetTokens() => _tokens;

        /// <summary>
        ///  Read value from given the key path.
        ///  if the key not exist, will return null.
        /// </summary>
        public IValueToken GetToken(string keyPath)
        {
            ArgumentNullException.ThrowIfNull(keyPath, nameof(keyPath));
            return GetTokenFromPath(keyPath);
        }

        /// <summary>
        ///  Read value list from given the key path.
        /// </summary>
        public IList<IValueToken> GetTokens(string keyPath)
        {
            ArgumentNullException.ThrowIfNull(keyPath, nameof(keyPath));

            return GetTokensFromPath(keyPath);
        }

        /// <summary>
        ///  Read all values from specific group key
        /// </summary>
        public GroupToken GetGroup(string key)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));
            return _tokens.OfType<GroupToken>().FirstOrDefault(x => x.Key == key);
        }

        /// <summary>
        ///  Read value from given the key path.
        ///  if the key not exist, will return null.
        /// </summary>
        public IValueToken this[string keyPath]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(keyPath, nameof(keyPath));
                return GetTokenFromPath(keyPath);
            }
        }

        /// <summary>
        ///  Add or update value by the key path
        /// </summary>
        /// <param name="keyPath">The key path</param>
        /// <param name="value">The string value</param>
        /// <param name="addAsGroup">Add as group when key path not found</param>
        /// <param name="comment">The comment</param>
        public NginxConfig AddOrUpdate(string keyPath, string value, bool addAsGroup = false, string comment = null)
        {
            ArgumentNullException.ThrowIfNull(keyPath, nameof(keyPath));

            var tokens = _tokens;

            var paths = keyPath.Split(':');

            GroupToken groupToken = null;

            int length = paths.Length;

            for (int i = 0; i < length; i++)
            {
                var (key, index) = ResolveKey(paths[i]);

                IToken find = null;
                IEnumerable<IToken> findTokens;
                if (groupToken == null)
                    findTokens = FindTokens(tokens, key) ;
                else
                    findTokens =FindTokens(groupToken.Tokens, key);
                var count = findTokens.Count();
                if (index > count)
                    throw new IndexOutOfRangeException($"The key '{key}' index must be <= {count}");

                if (index <= count - 1)
                    find = findTokens.ElementAt(index);

                if (find != null)
                {
                    if (find is ValueToken valueToken)
                        if (i == length - 1)
                        {
                            valueToken.Value = value;
                            valueToken.Comment = comment;
                            break;
                        }
                        else
                            throw new Exception($"The token '{find}' already exists.");

                    groupToken = (GroupToken)find;
                }
                else
                {
                    if (i == length - 1)
                    {
                        IValueToken newToken = new ValueToken(groupToken, key, value, comment);

                        if (addAsGroup)
                        {
                            newToken = new GroupToken(groupToken, key, value, comment);
                        }

                        if (groupToken == null)
                        {
                            tokens.Add(newToken);
                        }
                        else
                        {
                            groupToken.Add(newToken);
                        }
                    }
                    else
                    {
                        var newGroupToken = new GroupToken(groupToken, key);
                        if (groupToken == null)
                        {
                            tokens.Add(newGroupToken);
                        }
                        else
                        {
                            groupToken.Add(newGroupToken);
                        }
                        groupToken = newGroupToken;
                    }
                }
            }

            return this;
        }

        /// <summary>
        ///  Remove the value by key path
        /// </summary>
        public NginxConfig Remove(string keyPath)
        {
            ArgumentNullException.ThrowIfNull(keyPath, nameof(keyPath));

            var tokens = _tokens;

            var paths = keyPath.Split(':');

            GroupToken groupToken = null;

            int length = paths.Length;

            for (int i = 0; i < length; i++)
            {
                var (key, index) = ResolveKey(paths[i]);

                IToken find = null;
                IEnumerable<IToken> findTokens;

                if (groupToken == null)
                    findTokens = FindTokens(tokens, key);
                else
                    findTokens = FindTokens(groupToken.Tokens, key);

                if (i == length - 1)
                {
                    // remove
                    if (groupToken == null)
                    {
                        foreach (var item in findTokens)
                        {
                            tokens.Remove(item);
                        };
                    }
                    else
                       foreach (var item in findTokens)
                        {
                            groupToken.Tokens.Remove(item);
                        };
                }
                else
                {
                    var count = findTokens.Count();
                    if (index > count)
                        throw new IndexOutOfRangeException($"The key '{key}' index must be <= {count}");

                    if (index <= count - 1)
                        find = findTokens.ElementAt(index);

                    if (find != null && find is GroupToken token)
                    {
                        groupToken = token;
                    }
                    else
                    {
                        // not found , break.
                        break;
                    }
                }
            }

            return this;
        }

        /// <summary>
        ///  Save the configuration content to specific file
        /// </summary>
        /// <param name="fileName">The file path</param>
        public void Save(string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
            Save(fileName, Encoding.Default);
        }

        /// <summary>
        ///  Save the configuration content to specific file
        /// </summary>
        /// <param name="fileName">The file path</param>
        public Task SaveAsync(string fileName)
        {
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
            return SaveAsync(fileName, Encoding.Default);
        }

        /// <summary>
        ///  Save the configuration content to specific file
        /// </summary>
        /// <param name="fileName">The file path</param>
        /// <param name="encoding">The file encoding</param>
        public void Save(string fileName, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
            ArgumentNullException.ThrowIfNull(encoding, nameof(encoding));

            using StreamWriter fsWriter = new (fileName, false, encoding);
            WriteTokenString(_tokens, fsWriter, 0);
        }


         /// <summary>
        ///  Save the configuration content to specific file
        /// </summary>
        /// <param name="fileName">The file path</param>
        /// <param name="encoding">The file encoding</param>
        public async Task SaveAsync(string fileName, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
            ArgumentNullException.ThrowIfNull(encoding, nameof(encoding));
            await using StreamWriter fsWriter = new (fileName, false, encoding);
            WriteTokenString(_tokens, fsWriter, 0);
        }

        /// <summary>
        ///  Return configuration file content
        /// </summary>
        public override string ToString()
        {
            StringWriter sw = new (new StringBuilder());
            WriteTokenString(_tokens, sw, 0);
            return sw.GetStringBuilder().ToString();
        }

        private static void WriteTokenString(IEnumerable<IToken> tokens, TextWriter textWriter, int level = 0)
        {
            textWriter.NewLine = Environment.NewLine;
            foreach (var token in tokens.Where(x => x is CommentToken || x is ValueToken))
            {
                if (token is CommentToken comment)
                    textWriter.WriteLine(PadLeftSpace(comment.ToString(), level));

                else if (token is ValueToken value)
                    textWriter.WriteLine(PadLeftSpace(value.ToString(), level));
            }

            foreach (var group in tokens.OfType<GroupToken>())
            {
                //if (group.Parent != null)
                textWriter.WriteLine();

                if (!string.IsNullOrWhiteSpace(group.Comment))
                    textWriter.WriteLine(PadLeftSpace($"{group.Key} {group.Value} {{ # {group.Comment}", level));
                else
                    textWriter.WriteLine(PadLeftSpace($"{group.Key} {group.Value} {{ ", level));

                WriteTokenString(group.Tokens, textWriter, level + 1);

                // end
                textWriter.WriteLine(PadLeftSpace("}", level));
            }
        }

        private static string PadLeftSpace(string text, int level = 0) => text.PadLeft(text.Length + level * 2, ' ');

        private IValueToken GetTokenFromPath(string keyPath)
        {
            var tokens = _tokens;

            var paths = keyPath.Split(':');

            IValueToken result = null;

            foreach (var key in paths)
            {
                var (keyName, index) = ResolveKey(key);

                result = FindToken(tokens, keyName, index);
                if (result != null)
                {
                    if (result is GroupToken groupToken)
                        tokens = groupToken.Tokens.ToArray();
                }
                else
                    break;
            }

            return result;
        }

        private IList<IValueToken> GetTokensFromPath(string keyPath)
        {
            var tokens = _tokens;

            var paths = keyPath.Split(':');

            IEnumerable<IValueToken> result = null;

            for (int i = 0; i < paths.Length; i++)
            {
                var (keyName, index) = ResolveKey(paths[i]);

                if (i == paths.Length - 1)
                {
                    result = FindTokens(tokens, keyName);
                }
                else
                {
                    var current = FindToken(tokens, keyName, index);

                    if (current != null && current is GroupToken groupToken)
                    {
                        tokens = groupToken.Tokens.ToArray();
                    }
                }
            }
            return result.ToArray();
        }

        private static IValueToken FindToken(IEnumerable<IToken> tokens, string key, int index = 0) =>
             tokens.OfType<IValueToken>().Where(x => x.Key == key).ElementAtOrDefault(index);

        private static IEnumerable<IValueToken> FindTokens(IEnumerable<IToken> tokens, string key) =>
             tokens.OfType<IValueToken>().Where(x => x.Key == key);

        private static (string key, int index) ResolveKey(string key)
        {
            if (!Regex.IsMatch(key, @"^[\w\s]+(\[\d+\])?$"))
            {
                throw new Exception($"The key '{key}' format is incorrect");
            }

            var numberStartSymbol = key.IndexOf('[');

            var index = 0;
            string keyName = key;

            if (numberStartSymbol > 0)
            {
                var numberStartIndex = numberStartSymbol + 1;

                if (!int.TryParse(key[numberStartIndex..^1], out index))
                {
                    // TODO
                }

                keyName = key[..numberStartSymbol];
            }

            return (keyName, index);
        }

    }
}
