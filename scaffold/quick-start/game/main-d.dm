// main-d.dm: Interconnecting Procedures.

// When cheese hits the floor, the rodents scurry towards it!

obj
	cheese
		Move()
/*
			First, do the default movement operation.
			The variable '.' is a convenient way to set the return
			value before you are ready to return the procedure.
*/
			. = ..()

			for(var/mob/rat/rat in view(loc))
				walk_to(rat, loc, 1, 5)

/*
	The following code defines a new procedure for all physical
	objects: area, turf, obj, and mob. Notice what you get when
	you take the first letter from each of those words: atom!
*/

atom
	proc
		// By default, this procedure does nothing.
		Bumped(atom/movable/bumper)

mob
	player
		Bump(atom/obstacle)
/*
			The .. procedure will call the parent's return value.
			In this case, it will perform the actions we defined
			for Bump() in main-b.dm.
*/
			..()

/*
			This will call obstacle's (the object the player bumped
			into) Bumped() procedure. It passes the player into it
			as an argument.
*/
			obstacle.Bumped(src)

	rat
		Bumped(atom/movable/bumper)
			bumper << "The giant rat defends its territory ferociously!"

			dir = get_dir(src, bumper)

			flick("fight", src)