using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Lucene.Unity {

    /// <summary>
    /// A wrapper around Lucene.NET to provide a simplified interface for the most common use-cases.
    /// Additionally, this provides coroutines for indexing to enable Unity style cooperative multi-tasking.
    /// </summary>
    public class Lucene3D {

        /// <summary>
        /// Create a new instance of Lucene3D that is intended to be stored and used throughout the life of the program.
        /// When running on iOS, this constructor must be called from the main thread.
        /// </summary>
        /// <param name="name">The name of the index directory, the default 'index' is recommended.</param>
        public Lucene3D(string name = "index", bool deleteIfExists = false) {
            if(!ValidCrossPlatformName(name)) {
                throw new ArgumentException($"In order to enable the best cross platform ability, names are limited to {validNamePattern}", nameof(name));
            }
            indexDirectory = new DirectoryInfo(Path.Combine(Application.persistentDataPath, name));
            if (indexDirectory.Exists && deleteIfExists)
            {
	            indexDirectory.Delete(true);
	            indexDirectory.Create();
            }
        }

        /// <summary>
        /// Define index text through the use of a lambda expression that can extract the text from a given object of type T.
        /// Whenever a T is indexed, this lambda will be called to extract the appropriate text for indexing, enabling the central definition of indexes.
        /// Multiple types can be stored together in a single index, create the appropriate fields for each type.
        /// To ensure that indexes can be updated (without being complete reconstructed), define a IndexOptions.PrimaryKey for each T.
        /// </summary>
        /// <typeparam name="T">The type of indexed objects that this field is applied to.</typeparam>
        /// <param name="name">The name of field that is used by Lucene.</param>
        /// <param name="indexer">The lambda that extracts the text for indexing from T.</param>
        /// <param name="options">The storages options for the field that are passed to Lucene for storage and indexing.</param>
        public void DefineIndexField<T>(string name, Func<T, string> indexer, IndexOptions options) {
            if(indexer == null) {
                throw new ArgumentNullException(nameof(indexer));
            }
            var type = typeof(T);
            if(!indexers.ContainsKey(type)) {
                var newDefn = new TypeDefinition();
                indexers.Add(type, newDefn);
            }
            var typeDefinition = indexers[type];
            var indexDefinition = new IndexDefinition { Name = name, Indexer = TypedToUntypedLambda(indexer), Options = options };
            if(options == IndexOptions.PrimaryKey) {
                if(typeDefinition.PrimaryKey != null) {
                    throw new ArgumentException("Option IndexOptions.PrimaryKey can only be specified for one index.", nameof(options));
                }
                typeDefinition.PrimaryKey = indexDefinition;
            }
            if(options == IndexOptions.IndexTerms || options == IndexOptions.IndexTermsAndStore) {
                if(!defaultFields.Any(e => e == name)) {
                    var fieldsList = defaultFields.ToList();
                    fieldsList.Add(name);
                    defaultFields = fieldsList.ToArray();
                }
            }
            typeDefinition.Indexers.Add(indexDefinition);
        }

        /// <summary>
        /// Index the given object, where the text to be indexed must have been previously defined using DefineIndexField.
        /// </summary>
        /// <typeparam name="T">The implicit type of the item.</typeparam>
        /// <param name="item">The item to be indexed.</param>
        public void Index<T>(T item) {
            if(item == null) {
                throw new ArgumentNullException(nameof(item));
            }
            IndexInternal(new T[] { item }, processYields: false);
        }

        /// <summary>
        /// Index the given objects, where the text to be indexed must have been previously defined using DefineIndexField.
        /// </summary>
        /// <typeparam name="T">The implicit type of the items.</typeparam>
        /// <param name="items">The list of items to be indexed.</param>
        public void Index<T>(IEnumerable<T> items) {
            IndexInternal(items, processYields: false);
        }

        /// <summary>
        /// Index the given objects, where the text to be indexed must have been previously defined using DefineIndexField.
        /// </summary>
        /// <typeparam name="T">The implicit type of the items.</typeparam>
        /// <param name="items">The list of items to be indexed.</param>
        /// <param name="timeSlice">The maximum amount of time before the coroutine yields for other processing.  Use to strike a balance between not dropping frames and indexing performance.</param>
        public IEnumerator IndexCoroutine<T>(IEnumerable<T> items, int timeSlice = 13) {
            yield return IndexInternal<T>(items, timeSlice, true);
        }

        private IEnumerator IndexInternal<T>(IEnumerable<T> items, int timeSlice = 13, bool processYields = true) {
            if(items == null) {
                throw new ArgumentNullException(nameof(items));
            }
            var type = typeof(T);
            if(!indexers.ContainsKey(type)) {
                throw new ArgumentOutOfRangeException(nameof(items), "At least one index must be defined using DefineIndexTerm for a type before it can be indexed.");
            }
            var keyIndexer = indexers[type].PrimaryKey;
            var definitions = indexers[type].Indexers;
            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var count = 0;
            var notNullItems = items.Where(e => e != null);
            var total = notNullItems.Count();
            OnProgress("Indexing", 0, total);
            var directory = SimpleFSDirectory.Open(indexDirectory);
            var create = !IndexReader.IndexExists(directory);
            using(var writer = new IndexWriter(directory, analyzer, create, IndexWriter.MaxFieldLength.LIMITED)) {
                foreach(var item in notNullItems) {
                    var doc = new Document();
                    Term term = null;
                    if(keyIndexer != null) {
                        term = new Term(keyIndexer.Name, keyIndexer.Indexer(item));
                    }
                    foreach(var definition in definitions) {
                        var value = definition.Indexer(item);
                        var field = new Field(definition.Name, value, definition.StoreType(), definition.IndexType());
                        doc.Add(field);
                    }
                    if(term != null) {
                        writer.UpdateDocument(term, doc);
                    }
                    else {
                        writer.AddDocument(doc);
                    }
                    if(stopwatch.ElapsedMilliseconds >= timeSlice) {
                        OnProgress("Indexing", count, total);
                        if(processYields) {
                            yield return null;
                        }
                        stopwatch.Restart();
                    }
                    ++count;
                }
                OnProgress("Indexing", count, total);
                if(processYields) {
                    yield return null;
                }
                OnProgress("Optimizing", 0, 1);
                if(processYields) {
                    yield return null;
                }
                writer.Optimize();
                writer.Commit();
                OnProgress("Optimizing", 1, 1);
            }
        }

        public IEnumerable<Document> Search(string expression, bool allowLeadingWildcard = false, int maxResults = 100) {
            if(expression == null) {
                throw new ArgumentNullException(nameof(expression));
            }

            var directory = SimpleFSDirectory.Open(indexDirectory);
            using(var indexReader = IndexReader.Open(directory, true)) {
                using(var indexSearcher = new IndexSearcher(indexReader)) {
                    using(var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30)) {
                        var parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, defaultFields.ToArray(), analyzer);
                        parser.AllowLeadingWildcard = allowLeadingWildcard;
                        //var parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "headline", analyzer);
                        var query = parser.Parse(expression);
                        var hits = indexSearcher.Search(query, null, maxResults).ScoreDocs;
                        var docs = hits.Select(e => indexSearcher.Doc(e.Doc)).ToList(); // Need ToList as indexSearcher will be disposed...
                        return docs;
                    }
                }
            }

        }

        private string[] defaultFields = new string[0];

        /// <summary>
        /// The progress event provides callbacks for updates on processing within Lucene3D.
        /// Typically used to provide user feedback on long running Index operations over large data sets.
        /// </summary>
        public event EventHandler<LuceneProgressEventArgs> Progress;
        private LuceneProgressEventArgs progressEventArgs = new LuceneProgressEventArgs();
        private Stopwatch progressStopwatch = new Stopwatch();
        private void OnProgress(string title, int count, int total) {
            if(count == 0) {
                progressStopwatch.Restart();
            }
            if(Progress != null) {
                progressEventArgs.Title = title;
                progressEventArgs.Count = count;
                progressEventArgs.Total = total;
                progressEventArgs.Duration = (int)progressStopwatch.ElapsedMilliseconds;
                Progress(this, progressEventArgs);
            }
        }

        private bool ValidCrossPlatformName(string name) {
            return validName.IsMatch(name);
        }
        private const string validNamePattern = "[a-zA-Z0-9_]{1,64}";
        private Regex validName = new Regex(validNamePattern);

        private Func<object, string> TypedToUntypedLambda<T>(Func<T, string> func) {
            if(func == null) {
                return null;
            }
            else {
                return new Func<object, string>(o => func((T)o));
            }
        }

        private DirectoryInfo indexDirectory;

        private Dictionary<Type, TypeDefinition> indexers = new Dictionary<Type, TypeDefinition>();

        private class TypeDefinition {
            public IndexDefinition PrimaryKey { get; set; }
            public List<IndexDefinition> Indexers { get; } = new List<IndexDefinition>();
        }

        private class IndexDefinition {
            public string Name { get; set; }
            public Func<object, string> Indexer { get; set; }
            public IndexOptions Options { get; set; }
            public Field.Store StoreType() {
                return Options == IndexOptions.IndexTerms ? Field.Store.NO : Field.Store.YES;
            }
            public Field.Index IndexType() {
                switch(Options) {
                    case IndexOptions.IndexTermsAndStore:
                        return Field.Index.ANALYZED;
                    case IndexOptions.IndexTerms:
                        return Field.Index.ANALYZED;
                    case IndexOptions.IndexTextAndStore:
                        return Field.Index.NOT_ANALYZED;
                    case IndexOptions.StoreOnly:
                        return Field.Index.NO;
                    default:
                        return Field.Index.ANALYZED;
                }
            }
        }
    }

}
