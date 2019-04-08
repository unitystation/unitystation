using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Unity {

    /// <summary>
    /// The options for storing and indexing the data.
    /// </summary>
    public enum IndexOptions {

        /// <summary>
        /// This index represents the unique key for the object.  When indexing objects that have the same unique key, 
        /// the old object is removed from the index and the new one is added.  
        /// While not required, it is recommended that every type of object has a primary key index defined.
        /// </summary>
        PrimaryKey,

        /// <summary>
        /// Indexes and stores the entire text of the field as a single index term.
        /// Use for product IDs, GUIDs, SKUs, etc.
        /// </summary>
        IndexTextAndStore,

        /// <summary>
        /// Tokenizes the text into terms and indexes each term.  The full text is not stored.
        /// </summary>
        IndexTerms,

        /// <summary>
        /// Tokenizes the text into terms and indexes each term.  Additionally, the full text is stored in the index.
        /// Use for names and titles that are displayed in search results.
        /// </summary>
        IndexTermsAndStore,

        /// <summary>
        /// Does not index the text for searching, but stores it for returning with the document.
        /// </summary>
        StoreOnly,
    }

}