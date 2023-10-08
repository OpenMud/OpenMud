// main-e.dm: Adding Multiplayer Functionality.

/*
	Here are some "social" verbs. Notice how a double quote has to
	be "escaped" when it is inside of a text string.
*/

mob
	player
		verb
			// Conversation.
			say(msg as text)
				view(src) << "[src] says, \"[msg]\""

			shout(msg as text)
				world << "[src] shouts, \"[msg]\""

			whisper(msg as text)
				view(1) << "[src] whispers, \"[msg]\""

			tell(mob/m in world, msg as text)
				src << "You tell [m], \"[msg]\""

				m << "[src] tells you, \"[msg]\""

			// Configuration.
/*
	The argument name expects a text string in the prompt. Entering
	a name will set src.name to what you enter.
*/
			change_name(name as text)
				src.name = name
/*
	The argument icon expects an image file to be selected from
	the graphical interface that pops up. Supported formats
	include .dmi, .bmp, .png, .gif, .jpg, and .jpeg.
*/
			change_icon(icon as icon)
				src.icon = icon