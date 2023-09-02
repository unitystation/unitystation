using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Logs;
using SecureStuff;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecureStuff
{
	public static class Librarian
	{
		public static Library library = new Library();

		public static Dictionary<ulong, Library.LibraryBookShelf> IDToBookShelf =
			new Dictionary<ulong, Library.LibraryBookShelf>();

		public static Dictionary<ulong, Book> IDToBook = new Dictionary<ulong, Book>();
		public static Dictionary<ulong, Page> IDToPage = new Dictionary<ulong, Page>();

		private static ulong BookShelfAID = 1;
		private static ulong BookAID = 1;
		private static ulong PageAID = 1;

		public static Dictionary<Transform, Library.LibraryBookShelf> TransformToBookShelf =
			new Dictionary<Transform, Library.LibraryBookShelf>();

		private static Dictionary<MonoBehaviour, Book> MonoBehaviourToBook = new Dictionary<MonoBehaviour, Book>();
		private static Dictionary<object, Book> ObjectToBook = new Dictionary<object, Book>();

		private static Type TupleTypeReference = Type.GetType("System.ITuple, mscorlib");


		private static ICustomSerialisationSystem vvUIElementHandler;

		private static ICustomSerialisationSystem VVUIElementHandler
		{
			get
			{
				if (vvUIElementHandler == null)
				{
					// Load all assemblies in the current application domain
					Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

					// Iterate through each assembly
					foreach (var assembly in assemblies)
					{
						// Get types from the assembly that implement IMyInterface
						var types = assembly.GetTypes()
							.Where(type => typeof(ICustomSerialisationSystem).IsAssignableFrom(type));

						// Iterate through each type that implements the interface
						foreach (var type in types)
						{
							// Create an instance of the type (assuming it has a parameterless constructor)
							vvUIElementHandler = Activator.CreateInstance(type) as ICustomSerialisationSystem;
						}
					}
				}

				return vvUIElementHandler;
			}
		}

		public static void Reset()
		{
			library = new Library();
			ObjectToBook.Clear();
			MonoBehaviourToBook.Clear();
			TransformToBookShelf.Clear();
			BookShelfAID = 1;
			BookAID = 1;
			PageAID = 1;

			IDToPage.Clear();
			IDToBook.Clear();
			IDToBookShelf.Clear();
		}


		private static Book GetAttributes(Book Book, object Script)
		{
			if (HubValidation.TrustedMode == false) return null;
			Type monoType = Script.GetType();
			var Fields = monoType.BaseType.GetFields(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
				BindingFlags.FlattenHierarchy
			);

			var coolFields = Fields.ToList();

			coolFields.AddRange((monoType.GetFields(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
				BindingFlags.FlattenHierarchy
			).ToList()));

			foreach (FieldInfo Field in coolFields)
			{
				if (Field.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
				{
					Page Page = new Page();
					Page.VariableName = Field.Name;
					Page.ID = PageAID;
					PageAID++;
					Page.Info = Field;
					Page.Variable = Field.GetValue(Script);
					if (Page.Variable == null)
					{
						Page.Variable = "null";
					}

					Page.VariableType = Field.FieldType;
					Page.BindedTo = Book;
					IDToPage[Page.ID] = Page;
					Page.Sentences = new Librarian.Sentence();
					Page.Sentences.SentenceID = Page.ASentenceID;
					Page.ASentenceID++;
					var attribute = Field.GetCustomAttributes(typeof(VVNote), true);
					if (attribute.Length > 0)
					{
						var VVNoteAttributes = attribute.Cast<VVNote>().ToArray()[0];
						Page.VVHighlight = VVNoteAttributes.variableHighlightl;
					}

					GenerateSentenceValuesforSentence(Page.Sentences, Field.FieldType, Page, Script, FInfo: Field);
					Book.BindedPagesAdd(Page);
				}
			}

			if (TupleTypeReference !=
			    monoType) //Causes an error if this is not here and Tuples can not get Custom properties so it is I needed to get the properties
			{
				var Propertie = monoType.BaseType.GetProperties(
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
					BindingFlags.FlattenHierarchy
				);

				var coolProperties = Propertie.ToList();

				coolProperties.AddRange((monoType.GetProperties(
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
					BindingFlags.FlattenHierarchy
				).ToList()));
				foreach (PropertyInfo Properties in coolProperties)
				{
					if (Properties.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
					{
						if (Properties.CanRead == false)
						{
							continue; //TODO maybe UI indication and then sitting directly idk
						}

						Page Page = new Page();
						Page.VariableName = Properties.Name;

						Page.Variable = Properties.GetValue(Script);
						Page.VariableType = Properties.PropertyType;
						Page.PInfo = Properties;
						if (Page.Variable == null)
						{
							Page.Variable = "null";
						}

						Page.ID = PageAID;
						PageAID++;
						Page.BindedTo = Book;
						IDToPage[Page.ID] = Page;
						Page.Sentences = new Librarian.Sentence();
						Page.Sentences.SentenceID = Page.ASentenceID;
						Page.ASentenceID++;

						var attribute = Properties.GetCustomAttributes(typeof(VVNote), true);
						if (attribute.Length > 0)
						{
							var VVNoteAttributes = attribute.Cast<VVNote>().ToArray()[0];
							Page.VVHighlight = VVNoteAttributes.variableHighlightl;
						}

						GenerateSentenceValuesforSentence(Page.Sentences, Properties.PropertyType, Page, Script,
							PInfo: Properties);
						Book.BindedPagesAdd(Page);
					}
				}
			}

			var coolMethods = (monoType.GetMethods(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
				BindingFlags.FlattenHierarchy
			).ToList());

			foreach (MethodInfo Method in coolMethods)
			{
				if (Method.GetParameters().Length > 0) continue;
				Page Page = new Page();
				Page.VariableName = Method.Name;
				Page.ID = PageAID;
				PageAID++;
				Page.MInfo = Method;

				if (Page.Variable == null)
				{
					Page.Variable = "null";
				}

				Page.BindedTo = Book;
				IDToPage[Page.ID] = Page;
				Page.Sentences = new Librarian.Sentence();
				Page.Sentences.SentenceID = Page.ASentenceID;
				Page.ASentenceID++;


				var attribute = Method.GetCustomAttributes(typeof(VVNote), true);
				if (attribute.Length > 0)
				{
					var VVNoteAttributes = attribute.Cast<VVNote>().ToArray()[0];
					Page.VVHighlight = VVNoteAttributes.variableHighlightl;
				}

				// GenerateSentenceValuesforSentence(Page.Sentences, Field.FieldType, Page, Script, FInfo: Field);
				Book.BindedPagesAdd(Page);
			}

			return (Book);
		}

		private static void GenerateSentenceValuesforSentence(Sentence sentence, Type VariableType, Page Page,
			object Script,
			FieldInfo FInfo = null, PropertyInfo PInfo = null)
		{
			if (HubValidation.TrustedMode == false) return;
			if (FInfo == null && PInfo == null)
			{
				foreach (FieldInfo Field in VariableType.GetFields(
					         BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic |
					         BindingFlags.Static |
					         BindingFlags.FlattenHierarchy
				         ))
				{
					if (Field.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
					{
						if (Field.FieldType.IsGenericType)
						{
							IEnumerable list = Field.GetValue(Script) as IEnumerable;
							if (sentence.Sentences == null)
							{
								sentence.Sentences = new List<Sentence>();
							}

							uint count = 0;
							if (list != null)
							{
								foreach (var c in list)
								{
									Sentence _sentence = new Sentence();
									_sentence.ValueVariable = c;
									_sentence.OnPageID = Page.ID;
									_sentence.PagePosition = count;
									_sentence.ValueVariableType = c.GetType();
									_sentence.SentenceID = Page.ASentenceID;
									Page.ASentenceID++;
									Page.IDtoSentence[_sentence.SentenceID] = _sentence;
									Type valueType = c.GetType();
									if (valueType.IsGenericType)
									{
										Type baseType = valueType.GetGenericTypeDefinition();
										if (baseType == typeof(KeyValuePair<,>))
										{
											_sentence.KeyVariable = valueType.GetProperty("Key").GetValue(c, null);
											_sentence.ValueVariable = valueType.GetProperty("Value").GetValue(c, null);

											_sentence.ValueVariableType = valueType.GetGenericArguments()[1];
											_sentence.KeyVariableType = valueType.GetGenericArguments()[0];
										}
									}

									if (!valueType.IsClass)
									{
										GenerateSentenceValuesforSentence(_sentence, c.GetType(), Page, c);
									}

									count++;
									sentence.Sentences.Add(_sentence);
								}
							}
						}
					}
				}
			}
			else
			{
				if (VariableType.IsGenericType)
				{
					IEnumerable list;
					if (FInfo == null)
					{
						list = PInfo.GetValue(Script) as IEnumerable; //icollection<keyvaluepair>
					}
					else
					{
						list = FInfo.GetValue(Script) as IEnumerable; //
					}

					sentence.Sentences = new List<Sentence>();
					uint count = 0;
					if (list != null)
					{
						foreach (object c in list)
						{
							if (c == null) continue;

							Sentence _sentence = new Sentence();
							_sentence.ValueVariable = c;
							_sentence.OnPageID = Page.ID;
							_sentence.PagePosition = count;
							_sentence.ValueVariableType = c.GetType();
							_sentence.SentenceID = Page.ASentenceID;
							Page.ASentenceID++;
							Page.IDtoSentence[_sentence.SentenceID] = _sentence;

							Type valueType = c.GetType();
							if (valueType.IsGenericType)
							{
								Type baseType = valueType.GetGenericTypeDefinition();
								if (baseType == typeof(KeyValuePair<,>))
								{
									_sentence.KeyVariable = valueType.GetProperty("Key").GetValue(c, null);
									_sentence.ValueVariable = valueType.GetProperty("Value").GetValue(c, null);

									_sentence.ValueVariableType = valueType.GetGenericArguments()[1];
									_sentence.KeyVariableType = valueType.GetGenericArguments()[0];
								}
							}

							if (!valueType.IsClass)
							{
								GenerateSentenceValuesforSentence(_sentence, c.GetType(), Page, c);
							}

							count++;
							Page.Sentences.Sentences.Add(_sentence);
						}
					}
				}
			}
		}

		public sealed class Library
		{
			public List<LibraryBookShelf> Roots = new List<LibraryBookShelf>();

			public Dictionary<Transform, LibraryBookShelf> TransformToBookShelves =
				new Dictionary<Transform, LibraryBookShelf>();

			public void TraverseHierarchy()
			{
				if (HubValidation.TrustedMode == false) return;
				List<Transform> Transforms = new List<Transform>();
				foreach (var KV in Librarian.library.TransformToBookShelves)
				{
					if (KV.Key == null)
					{
						Transforms.Add(KV.Key);
					}
				}

				foreach (var root in Roots.ToArray())
				{
					if (root.Shelf == null)
					{
						Roots.Remove(root);
					}
				}

				foreach (var TF in Transforms)
				{
					Librarian.library.TransformToBookShelves.Remove(TF);
					TransformToBookShelf.Remove(TF);
				}

				int countLoaded = SceneManager.sceneCount;
				Scene[] loadedScenes = new Scene[countLoaded];

				for (int i = 0; i < countLoaded; i++)
				{
					loadedScenes[i] = SceneManager.GetSceneAt(i);
				}

				foreach (var Scene in loadedScenes)
				{
					var roots = Scene.GetRootGameObjects();
					foreach (var root in roots)
					{
						RecursivePopulate(root.transform, null);
					}
				}
			}

			List<LibraryBookShelf> THISDestroy = new List<LibraryBookShelf>();
			List<Transform> Children = new List<Transform>();
			List<Transform> TOProcessAdd = new List<Transform>();
			List<LibraryBookShelf> TOProcessRemove = new List<LibraryBookShelf>();

			public void RecursivePopulate(Transform Object, Transform Parent)
			{
				if (HubValidation.TrustedMode == false) return;
				THISDestroy.Clear();
				Children.Clear();
				TOProcessAdd.Clear();
				TOProcessRemove.Clear();

				if (TransformToBookShelves.ContainsKey(Object))
				{
					if (Object.childCount > 0)
					{
						for (int i = 0; i < Object.childCount; i++)
						{
							Children.Add(Object.GetChild(i));
						}
					}

					var libraryBookShelf = TransformToBookShelves[Object];


					foreach (var Child in Children)
					{
						if (libraryBookShelf.Contains.Contains(Child) == false)
						{
							TOProcessAdd.Add(Child);
						}
					}

					bool DestroySelf = false;
					if (libraryBookShelf.Parent != Parent)
					{
						if (libraryBookShelf.ParentChange())
						{
							DestroySelf = true;
						}
					}

					foreach (var Child in libraryBookShelf.InternalContain)
					{
						if (Child.Shelf == null)
						{
							TOProcessRemove.Add(Child);
							continue;
						}

						if (Children.Contains(Child.Shelf.transform) == false)
						{
							TOProcessRemove.Add(Child);
						}
					}


					foreach (var Child in TOProcessRemove)
					{
						libraryBookShelf.Contains.RemoveAll(item => item == null);
						if (Child.Shelf != null)
						{
							libraryBookShelf.Contains.Remove(Child.Shelf.transform);
							if (TransformToBookShelves.ContainsKey(Child.Shelf.transform))
							{
								TransformToBookShelves[Child.Shelf.transform].ParentChange();
							}
						}
						else
						{
							Child.DestroySelf();
						}

						libraryBookShelf.InternalContain.Remove(Child);
					}


					foreach (var Child in TOProcessAdd)
					{
						libraryBookShelf.Contains.Add(Child);
						Children.Add(Child);
					}


					foreach (var _Shelf in THISDestroy)
					{
						_Shelf.DestroySelf();
					}

					if (DestroySelf)
					{
						libraryBookShelf.DestroySelf();
					}

					foreach (var Child in Children.ToArray())
					{
						RecursivePopulate(Child, Object);
					}
				}
				else
				{
					if (Object.childCount > 0)
					{
						for (int i = 0; i < Object.childCount; i++)
						{
							Children.Add(Object.GetChild(i));
						}
					}

					var libraryBookShelf = LibraryBookShelf.PartialGenerateLibraryBookShelf(Object, Parent, Children);
					TransformToBookShelves[Object] = libraryBookShelf;
					if (Parent == null)
					{
						Roots.Add(libraryBookShelf);
					}

					foreach (var Child in Children.ToArray())
					{
						RecursivePopulate(Child, Object);
					}
				}
			}


			public sealed class LibraryBookShelf
			{
				public Transform Parent;
				public List<Transform> Contains = new List<Transform>();

				public List<LibraryBookShelf> InternalContain = new List<LibraryBookShelf>();

				public bool IsPartiallyGenerated = true;
				public List<Book> HeldBooks = new List<Book>();

				public ulong ID;
				public string ShelfName;
				public bool IsEnabled;
				public GameObject Shelf;

				public void DestroySelf()
				{
					if (Shelf != null)
					{
						Librarian.library.TransformToBookShelves.Remove(Shelf.transform);
						TransformToBookShelf.Remove(Shelf.transform);
					}

					IDToBookShelf.Remove(ID);
					Librarian.library.Roots.Remove(this);
					Contains.Clear();
				}

				public static LibraryBookShelf PartialGenerateLibraryBookShelf(Transform _Transform)
				{
					if (HubValidation.TrustedMode == false) return null;
					List<Transform> Children = new List<Transform>();
					if (_Transform.childCount > 0)
					{
						for (int i = 0; i < _Transform.childCount; i++)
						{
							Children.Add(_Transform.GetChild(i));
						}
					}

					return PartialGenerateLibraryBookShelf(_Transform, _Transform.parent, Children);
				}


				public static LibraryBookShelf PartialGenerateLibraryBookShelf(Transform _Transform, Transform _Parent,
					List<Transform> Children)
				{
					if (HubValidation.TrustedMode == false) return null;
					if (library.TransformToBookShelves.ContainsKey(_Transform))
					{
						return (library.TransformToBookShelves[_Transform]);
					}

					var libraryBookShelf = new LibraryBookShelf();
					libraryBookShelf.ShelfName = _Transform.gameObject.name;
					libraryBookShelf.ID = BookShelfAID;
					BookShelfAID++;
					libraryBookShelf.Shelf = _Transform.gameObject;
					libraryBookShelf.Parent = _Parent;
					if (_Parent != null)
					{
						library.TransformToBookShelves[_Parent].InternalContain.Add(libraryBookShelf);
					}

					libraryBookShelf.Contains.AddRange(Children);
					IDToBookShelf[libraryBookShelf.ID] = libraryBookShelf;
					TransformToBookShelf[libraryBookShelf.Shelf.transform] = libraryBookShelf;

					return libraryBookShelf;
				}

				public bool ParentChange()
				{
					if (Shelf == null)
					{
						return false;
					}

					Parent = Shelf.transform.parent;
					if (Shelf.transform.parent != null)
					{
						library.TransformToBookShelves[Shelf.transform.parent].InternalContain.Add(this);
					}

					return true;
				}


				public void PopulateBookShelf()
				{
					if (HubValidation.TrustedMode == false) return;
					if (this.IsPartiallyGenerated == false)
					{
						return;
					}

					MonoBehaviour[] scriptComponents = Shelf.GetComponents<MonoBehaviour>();
					HeldBooks.Clear();
					foreach (MonoBehaviour mono in scriptComponents)
					{
						if (mono != null)
						{
							HeldBooks.Add(Book.PartialGeneratebook(mono));
						}
					}

					this.IsPartiallyGenerated = false;
				}

				public void UpdateBookShelf()
				{
					if (HubValidation.TrustedMode == false) return;
					if (IsPartiallyGenerated)
					{
						PopulateBookShelf();
						return;
					}
				}
			}
		}

		public sealed class Book
		{
			public ulong ID;
			public string Title;
			public object BookClass;
			public bool IsnotMono;

			private List<Page> _BindedPages = new List<Page>();

			public List<Page> BindedPages
			{
				get
				{
					if (UnGenerated)
					{
						Loggy.LogWarning("USE GetBindedPages()!,since these books are ungenerated ",
							Category.VariableViewer);
					}

					return _BindedPages;
				}
				set { _BindedPages = value; }
			}

			public bool UnGenerated = true;

			public List<Page> GetBindedPages()
			{
				if (HubValidation.TrustedMode == false) return null;
				if (UnGenerated)
				{
					if (BookClass != null)
					{
						PopulateBook(this);
					}
					else
					{
						Loggy.LogError("Book has been destroyed!" + ID, Category.VariableViewer);
					}

					UnGenerated = false;
				}

				return (_BindedPages);
			}

			public void BindedPagesAdd(Page page)
			{
				_BindedPages.Add(page);
			}


			public override string ToString()
			{
				StringBuilder logMessage = new StringBuilder();
				logMessage.AppendLine("Title > " + Title);
				logMessage.Append("Pages > ");
				logMessage.AppendLine(string.Join("\n", BindedPages));

				return (logMessage.ToString());
			}

			public static Book PopulateBook(Book book)
			{
				if (HubValidation.TrustedMode == false) return null;
				if (!book.UnGenerated)
				{
					return (book);
				}

				var mono = book.BookClass;
				book.UnGenerated = false;
				Type monoType = mono.GetType();
				book = GetAttributes(book, mono);
				return (book);
			}


			public static Book PartialGeneratebook(MonoBehaviour mono)
			{
				if (HubValidation.TrustedMode == false) return null;
				if (MonoBehaviourToBook.ContainsKey(mono))
				{
					return (MonoBehaviourToBook[mono]);
				}

				Book book = new Book();
				book.ID = BookAID;
				BookAID++;
				book.BookClass = mono;
				book.Title = mono.ToString();
				IDToBook[book.ID] = book;
				MonoBehaviourToBook[mono] = book;
				return (book);
			}

			public static Book GenerateNonMonoBook(object Eclass)
			{
				if (HubValidation.TrustedMode == false) return null;
				if (ObjectToBook.ContainsKey(Eclass))
				{
					return (ObjectToBook[Eclass]);
				}

				Type TType = Eclass.GetType();
				Book book = new Book();
				book.ID = BookAID;
				BookAID++;
				book.BookClass = Eclass;
				book.IsnotMono = true;
				book.UnGenerated = false;
				book.Title = Eclass.ToString();
				ObjectToBook[Eclass] = book;
				IDToBook[book.ID] = book;

				book = GetAttributes(book, Eclass);
				return (book);
			}
		}


		public sealed class Page
		{
			public ulong ID;
			public string VariableName;
			public object Variable;
			public Type VariableType;
			public string AssemblyQualifiedName;
			public Book BindedTo;
			public PropertyInfo PInfo;
			public bool PCanWrite => PInfo.CanWrite;

			public FieldInfo Info;

			public uint ASentenceID;
			public Sentence Sentences;

			public MethodInfo MInfo;

			public Dictionary<uint, Sentence> IDtoSentence = new Dictionary<uint, Sentence>();
			public VVHighlight VVHighlight = VVHighlight.None;

			public override string ToString()
			{
				return (VariableName + " = " + Variable + " of   " + VariableType);
			}

			public void SetValue(string Value)
			{
				if (HubValidation.TrustedMode == false) return;
				//Logger.Log(this.ToString());
				//Logger.Log(ID.ToString());
				//Logger.Log(Variable.GetType().ToString());
				try
				{
					if (PInfo != null)
					{
						PInfo.SetValue(BindedTo.BookClass, DeSerialiseValue(Variable, Value, Variable.GetType()));
					}
					else if (Info != null)
					{
						Info.SetValue(BindedTo.BookClass, DeSerialiseValue(Variable, Value, Variable.GetType()));
					}

					UpdatePage();
				}
				catch (ArgumentException exception)
				{
					Loggy.LogError(
						$"Catch Argument Exception for Variable Viewer {exception.Message} \n {exception.StackTrace}",
						Category.VariableViewer);
				}
			}

			public void Invoke()
			{
				if (HubValidation.TrustedMode == false) return;
				if (MInfo != null)
				{
					MInfo.Invoke(BindedTo.BookClass, null);
				}
			}


			public static object DeSerialiseValue(object InObject, string StringVariable, Type InType)
			{
				if (VVUIElementHandler.CanDeSerialiseValue(InType))
				{
					return VVUIElementHandler.DeSerialiseValue(StringVariable, InType);
				}
				else
				{
					if (InType.IsEnum)
					{
						//if ()
						return Enum.Parse(InObject.GetType(), StringVariable);
					}
					else
					{
						if (InType == null || InObject == null || InObject as IConvertible == null)
						{
							Loggy.Log($"Can't convert {StringVariable} to {InObject.GetType()}  " +
							           $"[(InType == null) = {InType == null} || (InObject == null) == {InObject == null} || (InObject as IConvertible == null) = {InObject as IConvertible == null}]",
								Category.VariableViewer);
							return null;
						}

						return Convert.ChangeType(StringVariable, InObject.GetType());
					}
				}
			}


			public void UpdatePage()
			{
				if (HubValidation.TrustedMode == false) return;
				if (PInfo != null)
				{
					Variable = PInfo.GetValue(BindedTo.BookClass);
				}

				if (Info != null)
				{
					Variable = Info.GetValue(BindedTo.BookClass);
				}

				if (Variable == null)
				{
					Variable = "null";
				}

				//GenerateSentenceValuesforSentence
				if (Sentences.Sentences != null)
				{
					IDtoSentence.Clear();
					Sentences = new Sentence();
					ASentenceID = 0;
					Sentences.SentenceID = ASentenceID;
					ASentenceID++;
					if (PInfo != null)
					{
						GenerateSentenceValuesforSentence(Sentences, PInfo.PropertyType, this, BindedTo.BookClass,
							PInfo: PInfo);
					}
					else
					{
						GenerateSentenceValuesforSentence(Sentences, Info.FieldType, this, BindedTo.BookClass,
							FInfo: Info);
					}
				}
			}
		}

		public sealed class Sentence
		{
			public uint SentenceID;
			public uint PagePosition;
			public object KeyVariable;
			public Type KeyVariableType;

			public object ValueVariable;
			public Type ValueVariableType;

			public List<Sentence> Sentences;
			public ulong OnPageID;
		}


		public static Type UEGetType(string TypeName)
		{
			if (TypeName == null || TypeName == "")
			{
				return null;
			}

			// Try Type.GetType() first. This will work with types defined
			// by the Mono runtime, in the same assembly as the caller, etc.
			var type = Type.GetType(TypeName);

			// If it worked, then we're done here
			if (type != null)
				return type;

			// If the TypeName is a full name, then we can try loading the defining assembly directly
			/*if (TypeName.Contains(".")) //We assume assemblies are already loaded
			{
				// Get the name of the assembly (Assumption is that we are using
				// fully-qualified type names)
				var assemblyName = TypeName.Substring(0, TypeName.IndexOf('.'));

				// Attempt to load the indicated Assembly
				if (assemblyName != "Unity" && assemblyName != "Light2D")
				{
					var assembly = Assembly.Load(assemblyName);

					// Ask that assembly to return the proper Type
					type = assembly.GetType(TypeName);
					if (type != null)
						return type;
				}

			}

			*/
			// If we still haven't found the proper type, we can enumerate all of the
			// loaded assemblies and see if any of them define the type
			var currentAssembly = Assembly.GetExecutingAssembly();
			var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
			foreach (var assemblyName in referencedAssemblies)
			{
				// Load the referenced assembly
				var assembly = Assembly.Load(assemblyName);
				if (assembly != null)
				{
					// See if that assembly defines the named type
					type = assembly.GetType(TypeName);
					if (type != null)
						return type;
				}
			}

			// The type just couldn't be found...
			return null;
		}

		private static Type GetUnderlyingType(this MemberInfo member)
		{
			if (HubValidation.TrustedMode == false) return null;
			switch (member.MemberType)
			{
				case MemberTypes.Event:
					return ((EventInfo) member).EventHandlerType;
				case MemberTypes.Field:
					return ((FieldInfo) member).FieldType;
				case MemberTypes.Method:
					return ((MethodInfo) member).ReturnType;
				case MemberTypes.Property:
					return ((PropertyInfo) member).PropertyType;
				default:
					throw new ArgumentException
					(
						"Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
					);
			}
		}

		private static object GetValue(this MemberInfo memberInfo, object forObject)
		{
			if (HubValidation.TrustedMode == false) return null;
			switch (memberInfo.MemberType)
			{
				case MemberTypes.Field:
					return ((FieldInfo) memberInfo).GetValue(forObject);
				case MemberTypes.Property:
					return ((PropertyInfo) memberInfo).GetValue(forObject);
				default:
					throw new NotImplementedException();
			}
		}

		private static void MemberInfoSetValue(this MemberInfo memberInfo, object ClassObject, object NewVariableObject)
		{
			if (HubValidation.TrustedMode == false) return;
			switch (memberInfo.MemberType)
			{
				case MemberTypes.Field:
					((FieldInfo) memberInfo).SetValue(ClassObject, NewVariableObject);
					break;
				case MemberTypes.Property:
					((PropertyInfo) memberInfo).SetValue(ClassObject, NewVariableObject);
					break;
				default:
					throw new NotImplementedException();
			}
		}
	}
}