# lucene-unity
Adaptation of Lucene.NET to work with Unity3D, including coroutine and behaviour facades.

## Installation
This repo is designed to be used as a submodule of an existing Unity3D repo.  E.g.

```
    git submodule add https://github.com/aakison/lucene-unity.git Assets/Lucene
```

There are no demo scenes or resources in this repo, just the required files so that your solution is not polluted with unnecessary files.

## Original Usage

Once installed, you have full access to the Lucene.NET port of the Lucene library.
For complete usage examples, please see the official Lucene documentation at the [http://lucene.apache.org/](http://lucene.apache.org/).

## Getting Started with Unity Wrapper

For simplicity, there is a wrapper designed for use with Unity which provides an easier interface for Unity apps.
To get started, use the `Lucene3D` class.

Before you start, create a POCO for the data that you want indexed along with any additional meta-data, e.g.:

```
    public class Article {
        public string Url { get; set; }
        public string Headline { get; set; }
        public string Body { get; set; }
    }
```

To start, create the `Unity3D` class, no parameters are required. 

```
    var lucene = new Lucene3D();
```

This will create the lucene index in the Unity Application.persistentDataPath directory.
This class is a long-life class and should be instantiated once for the life of the app.
Note that the constructor must be called on the main thread as Unity requires that Application is only accessed from that thread.

Next, define how you want elements of your POCO indexed and stored:

```
    lucene.DefineIndexField<Article>("url", e => e.Url, IndexOptions.PrimaryKey);
    lucene.DefineIndexField<Article>("headline", e => e.Headline, IndexOptions.IndexTermsAndStore);
    lucene.DefineIndexField<Article>("body", e => e.Body, IndexOptions.IndexTerms);
```

In this example, we index both the headline and the body. 
We also store the full text of the headline in the lucene index, allowing for immediate use when searching.
The body is not stored in the index and we need to store it somewhere else, e.g. the file system.

Then, add your corpus of articles to the index:

```
    var articles = GetArticlesFromSomewhere();

    // Option 1, one at a time:
    lucene.Index(articles[0]);

    // Option 2, let the wrapper add them (suitable for background thread)
    lucene.Index(articles);

    // Option 3, use coroutines for cooperative multi-tasking
    StartCoroutine(lucene.IndexCoroutine(articles));
```

Finally, search through the index using Search:

```
    var expression = searchBox.text;  // e.g. "star NEAR (wars OR trek)"
    var results = lucene.Search(expression);
```

The `results` contains a list of lucene `Document` which has the results.  
It does not return a `Article` POCO as this system indexes the article only and does not store the original POCO.
To retrieve the information from the `Document`, simply use the `Get` method:

```
    var headlines = results.Select(e => e.Get("headline"));
```

In addition to this primary use-case, we can also monitor progress using the `Progress` event.

## License Info

This software was originally published as Lucene from the Apache foundation. 
It was then ported into another open source project Lucene.NET. 
The lucene-unity version modifies it to work with Unity through non-functional 
changes to the core code (to remove warnings), and to add a wrapper to make 
consumption from Unity easier.

Finally, for unitystation, we did the following:
 - Remove empty catch blocks so we can determine what errors are happening
 - Provide the ability to delete the index
 - Allow wildcard searches with leading wildcard