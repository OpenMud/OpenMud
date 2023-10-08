import { SvrActionableCommands } from "../models/SvrActionableCommands";
import { NetComponent } from "./NetComponent";

export class NetActionableCommands extends NetComponent<'actionableCommands'> {
  public readonly type = 'actionableCommands';

  constructor(public readonly frame: number, public readonly exists: boolean, public readonly commands: SvrActionableCommands) {
    super();
  }
}
