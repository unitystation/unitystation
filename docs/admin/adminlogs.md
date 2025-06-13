# how use admin search

so on anything coloured blue,

 you can click on to expand

The information is

the objects relating to, 

the object it is stored in

the player account associated with the object

the objects position

so, 
if you click on it it will teleport you to that object

Now combining this with search,

Holding SHIFT

it will add it to the search,

 for an object this would be its netID

basically unique identifier, so it will bring up every single log to do with that object* (anything that contains the netid number

If you want to search where the object is the thing that's The storage if you hold TAB and SHIFT
on the Stored in the field

It will add StoredIn=56345 This is Touching direct structure of the logs to find every instance where,
the object Numbers stored Matches

A course you can just do a general string search e.g you want to look for bob
Everything with bob in will come up, if you want the direct log access

```json
{
    "LogTime": "2025-05-17T16:48:46.074619Z",
    "Log": [{
            "ObjectName": "Slime",
            "Object": 441,
            "StoredInName": "example",
            "StoredIn": 4294967295,
            "PlayerAccountID": "example",
            "PositionWorld": "45,110,0",
            "Info": ""
        }, {
            "ObjectName": "example",
            "Object": 4294967295,
            "StoredInName": "example",
            "StoredIn": 4294967295,
            "PlayerAccountID": "example",
            "PositionWorld": "0,0,0",
            "Info": "Slime has left FallStation"
        }
    ],
    "LogImportance": "MISC",
    "Category": "World"
}
```
#### note This is with nice formatting, So spaces have been added example of what the log looks like in the log file
```json
{"LogTime":"2025-05-17T16:48:46.074619Z","Log":[{"ObjectName":"Slime","Object":441,"StoredInName":null,"StoredIn":4294967295,"PlayerAccountID":null,"PositionWorld":"45,110,0","Info":""},{"ObjectName":null,"Object":4294967295,"StoredInName":null,"StoredIn":4294967295,"PlayerAccountID":null,"PositionWorld":"0,0,0","Info":"Slime has left FallStation"}],"LogImportance":"MISC","Category":"World"}

```

This is how your average log looks, For convenience sake you can use

(First what you put into Search, "->" Second What it gets turned into when searching,

x Donating input

)

ObjName=x -> "ObjectName":"x

Obj=x -> "Object":x

StoredInName=x -> "StoredInName":"x

StoredIn=x -> "StoredIn":x

PlayerAccount=x -> "PlayerAccountID":"x

Position=x -> "Position":"x

Info=x -> "Info":"x

## Search syntax

now, Let's say you want to do more advanced search this is where the search syntax comes in useful

### " AND "
If you want to look for

bob and jane 

What you would do is search for 

bob AND jane 

the " AND " Spaces either side is important

This will return logs that contain bob and jane

### " OR " 
not Let's say you want

Logs that jane or bob were involved in 
so You can do that by configuring

bob OR jane

And this will return logs with bob or Jane contained

### "{}" Brackets
now If you want a more complex scenario where 

jane or bob , Using Knife item

You can do 

{bob OR jane} AND Knife

so Return logs at contain bob OR jane , But also must include the knife

The {} are important for order of Search, 
Since without them the search could be  interpreted wrong e.g 

bob OR jane AND Knife

Could become bob  OR { jane AND Knife }

Where  It's stuff that contains bob OR  anything that contains  jane AND Knife

### " NOT "

so, Let's say you want to look for bob OR jane But you don't want anything to do with Knife coming back

You would do

{bob OR jane} AND NOT Knife

so, This will return where logs mention bob OR jane, But exclude everything with in Knife

