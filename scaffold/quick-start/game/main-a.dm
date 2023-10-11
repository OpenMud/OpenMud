/*
	Your First World

	Original Author: Dan of Dantom
	Maintainer: LordAndrew
	E-mail: lordovos@live.com
	Version: 6.1 (September 8th, 2012)

	General Notes

	1.	This section is commented out, which means it is ignored
		by the compiler. It is a multi-line comment, as it begins
		with /* and ends with */. To comment out single lines,
		you can use // as seen further below.

	2.	This tutorial includes a file called readme.html in the
		readme/ directory. Mark [] Show all files at the bottom
		of the File tile on the left of the window, then click
		the [+] next to the readme folder. Then simply
		double-click the readme.html file to open it.

	3.	To learn more about the built-in object types, variables,
		and procedures used in this tutorial, click on it and
		press F1 to read about it in the reference.
*/

// main-a.dm: Introduction to Objects and Variables.

/*
	The world object acts as a container for your game. It holds
	all of the objects derived from the atom tree. It also allows
	you to connect your game world to the hub, so that others
	may see it.
*/


world
	// Set the world's name. This will appear in the title bar.
	name = "Your First World"

	// Configure the default mob object players will log in as.
	mob = /mob/player

/*
	The mob object may be used to represent players or other
	computer-controlled personalities. By default, mobs are
	dense, meaning no two mobs can occupy the same location
	using convential movement procedures.
*/

mob
	player
		icon = 'player.dmi'

	rat
		icon = 'rat.dmi'

/*
	The turf object represents backdrops. They are effectively
	your tileset that players can explore.
	Here, we define two turfs: a floor that you can walk on, and
	a wall that blocks both your movement and view.
*/

turf
	floor
		icon = 'floor.dmi'

	wall
		icon = 'wall.dmi'
/*
		Density determines whether or not another dense, moving
		object may enter the tile.
*/
		density = 1

		// Opacity determines if something can be seen through.
		opacity = 1

/*
	The obj object is generally used for inanimate items.
	By default, it is not dense and will appear above turfs, but
	below mobs.
*/

obj
	cheese
		icon = 'cheese.dmi'

	scroll
		icon = 'scroll.dmi'

/*
	The area object type occupies an entire region of the map.
	The following areas are defined here to exist on the map for
	this tutorial, but they do not play an important role until
	later on in this demonstration.
*/

area
	outside

	cave