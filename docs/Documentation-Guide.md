### Documentation Guide

This page helps to explain how to create / edit documentation. Feel free to edit and expand this!

* [Our main docs site is here](https://unitystation.github.io/unitystation/)
* You're allowed to edit directly via github, you don't need to use PRs for docs changes. If you don't have permissions and are
planning to make significant changes, ask in Discord.
* You can edit any page by clicking the pencil icon in the top right of a page or by 
making a change on the `develop` branch within the `docs` folder
* If you have permissions you can create pages directly in GitHub by browsing under [the docs folder](https://github.com/unitystation/unitystation/tree/develop/docs)
* When you change docs, they will eventually be built and published to our docs site. Look in the `#git` channel for a `Branch gh-pages 
was force-pushed` message to see when they are published.
* For working on larger edits / pages, we recommend using VSCode or an appropriate text editor to edit these, use a Markdown plugin 
* We use `MkDocs` for our docs site, which internally uses `Python-Markdown` for rendering. Markdown renderers often have slight variations / 
conventions, so if you are running into trouble with something try searching "MkDocs (issue)" or "Python Markdown (issue)" to see the "right" way
to do something.
* Use 4 spaces for indentation.
* Code blocks don't work like how they do on github pages. For root-level (non-indented) codeblocks you can use "fenced code blocks" just like you can on github. But for code blocks appearing in a list you must use indented code blocks and a special macro. [See this page for an example of indented code blocks in action](https://raw.githubusercontent.com/unitystation/unitystation/develop/docs/development/SyncVar-Best-Practices-for-Easy-Networking.md)
