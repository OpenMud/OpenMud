import { SceneEvents } from "excalibur";

export class DispatchVerbMessage {
    public constructor(
        public readonly source : string,
        public readonly target : string | undefined | null,
        public readonly command : string | undefined,
    ) { }
}