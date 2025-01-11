import { HubConnection, HubConnectionBuilder, JsonHubProtocol } from "@microsoft/signalr";

export class MudDebugClient {

    private constructor(private hub: HubConnection) {
    }

    public static async create(host: string, port: number) : Promise<MudDebugClient> {
        const hub = new HubConnectionBuilder()
            .withUrl(`http://${host}:${port}/debugHub`)
            .withHubProtocol(new JsonHubProtocol())
            .build();

        await hub.start();

        return new MudDebugClient(hub);
    }

    public start() {
        this.hub.send("startup");
    }

    public close() {
        this.hub.send("shutdown");
        this.hub.start();
    }
}