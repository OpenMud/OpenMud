import { Vector } from "excalibur";

export class DispatchMovementMessage {
    public constructor(
        public readonly entity : string,
        public readonly movement : Vector
    ) { }
}