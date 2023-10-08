// main-c.dm: Introducing Verbs.

/*
	Verbs represent commands that players may use.
*/

obj
	verb
		get()
/*
			This makes it so that you have to be standing on top
			of the obj to pick it up. The oview() procedure is
			similar to view() in that it retrieves a list of
			everything in sight, but it doesn't include the center.
*/
			set src in oview(0)

			usr << "You get [src]."

			// Move the obj into the player's contents.
			Move(usr)

		drop()
			usr << "You drop [src]."

			Move(usr.loc)

	cheese
		desc = "It is quite smelly."

		verb
			eat()
				usr << "You take a bite of the cheese. Bleck!"

				suffix = "(nibbled)"

	scroll
		desc = "It looks to be rather old."

		verb
			read()
				usr << "You utter the phrase written on the scroll: \"Knuth\"!"

				// Create a new rat at the player's location.
				new /mob/rat(usr.loc)

				usr << "A giant rat appears!"

				// Delete the scroll after use.
				del src

mob
	player
		desc = "A handsome and dashing rogue."

		Stat()
			statpanel("Inventory", contents)

		verb
			look()
				src << "You see..."

				for(var/atom/movable/o in oview())
					src << "[o].  [o.desc]"

	rat
		desc = "It's quite large."