# Species Notes

### Prefab populater

so on the base torso prefab currently and planned for the future, There are spare slots that are used by Custom body parts on different races,

Unity has a Weird way of handling lists With prefab variants, 
So you adding a new body part on index 7 on your custom species, when the base is only up to 6, 
that when you modify the base  adding a new one in making it index 7 , 
Your custom body part on index 7 will override the base prefab,

so The solution is to have big empty list, Base prefab will populate from the bottom,
Custom races will populate from the top, 
This should reduce conflicts