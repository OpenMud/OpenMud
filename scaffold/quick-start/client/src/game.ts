import { MudGame } from "openmud";
import {GameIconAnimationsIndex, GameIcons, GameResourceLoader} from './resources';

export class Game extends MudGame {
  constructor(canvas: string) {
    super(canvas, GameResourceLoader, GameIcons, GameIconAnimationsIndex);
  }
}