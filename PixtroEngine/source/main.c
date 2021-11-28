#include <maxmod.h>
#include "tonc_vscode.h"

#include "soundbank.h"
#include "soundbank_bin.h"

#include "core.h"

int main()
{
	// Mute game until ready
	REG_SNDDSCNT = 0;

	// Initialize maxmod with the soundbank
	mmInitDefault((mm_addr)soundbank_bin, AUDIO_CHANNELS);

	// Add maxmod VBlank interrupt
	irq_init(NULL);
	irq_add(II_VBLANK, mmVBlank);

	// Initialize the game engine
	pixtro_init();

#ifdef __DEBUG__
	SET_DEBUGFLAG(WAITING);
#endif

	// Enable all channels for audio (the music and sfx aren't loud enough otherwise. might look into that later)
	REG_SNDDSCNT |= SDS_AR | SDS_AL | SDS_BR | SDS_BL;

	// Game loop
	while (1)
	{
#ifdef __DEBUG__
		while (!ENGINE_DEBUGFLAG(READY))
		{
			mmFrame();
			VBlankIntrWait();
		}
#endif

		// Get input
		key_poll();

		// Update the engine
		pixtro_update();

		// Update maxmod before V sync to get max time for rendering
		mmFrame();
		VBlankIntrWait();

		// Render game
		pixtro_render();
	}
}