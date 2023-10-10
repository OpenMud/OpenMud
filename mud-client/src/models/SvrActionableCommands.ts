export interface SvrCommandDetails {
  precedent: number;
  target: string | undefined;
  targetName: string | undefined;
  verb: string;
}

export interface SvrActionableCommands {
    commands: SvrCommandDetails[];
}
