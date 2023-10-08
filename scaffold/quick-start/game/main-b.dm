// main-b.dm: Introducing Procedures.

/*
	The following code defines a new variable for all area
	objects. It is called music and will contain a sound file to
	be played when a player enters an area. We also set a built-in
	variable, desc, to describe the area.
*/

area
	var
		music

	Entered(mob/m)
/*
		In this procedure, we use the argument m of the type mob.
		We then use the built-in procedure ismob() to make sure
		that m really is a mob. If it isn't, we return the proc
		which causes it to do nothing.
*/
		if(!ismob(m))
			return
/*
		If we made it this far, that means the m that Entered()
		the area was indeed a mob. We output to them the area's
		description and play them some cool tunes.
*/
		m << desc

		m << sound(music, 1, channel = 1)

	outside
		desc = "Nice and jazzy, here..."

		music = 'jazzy.mid'

	cave
		desc = "Watch out for the giant rat!"

		music = 'cavern.mid'

mob
	player
		Bump(atom/obstacle)
			src << "You bump into [obstacle]."

			src << 'ouch.wav'