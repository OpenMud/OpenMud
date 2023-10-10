import { createRoot } from "react-dom/client";
import { React } from 'react-dom';
import { Dispatch, SetStateAction, memo, useEffect, useState } from 'react';

import { Game } from './game'

import { CommandBar } from 'openmud';
import { ICommandInteractionWidget, NetPlayerControlSystem } from "openmud";
import { SvrActionableCommands } from "openmud";

const container = document.getElementById("app");
const root = createRoot(container)

class ActionableCommandsListener implements ICommandInteractionWidget {
    private playerControlSystem: NetPlayerControlSystem | undefined;
    private updateCommands: Dispatch<SetStateAction<any>> | undefined = undefined;
    private _enableGameInput: boolean = true;
    private _activeEntity: string | undefined = undefined;;

    public constructor() {
    }

    set enableGameInput(value: boolean) {
        this._enableGameInput = value;
    }

    get enableGameInput(): boolean {
        return this._enableGameInput;
    }

    activeEntity(): string | undefined {
        return this._activeEntity;
    }

    configureUpdateCommands(setCommands: Dispatch<SetStateAction<any>>) {
        this.updateCommands = setCommands;
    }

    updateSelected(command: any) {
        this._activeEntity = command?.subject_entity;
    }

    availableCommands(commands: SvrActionableCommands | undefined): void {
        if (!this.updateCommands)
            return;

        if (commands == undefined) {
            this.updateCommands([]);
            return;
        }

        const cmds = commands!.commands.map(c => {
            return {
                id: c.verb + c.target,
                name: c.verb + (c.targetName ? (" " + c.targetName) : ""),
                subject_entity: c.target,
                command: () => {console.log("Item command triggered...")},
            };
        });

        this.updateCommands(cmds);
    }

}


let game : Game;
const actionListener = new ActionableCommandsListener();

function processCommand(processCommand: any, additionalText: string) {
    const entity = processCommand.subject_entity;
    const commandString = processCommand.name + additionalText;

    game.dispatchCommand(entity, commandString);
}

const Viewport = memo(() => {
    useEffect(() => {
        game = new Game("viewport");
        game.initialize(actionListener);

        return () => {
            game.stop();
        }
    })
    
    return <canvas id="viewport"></canvas>
});

export function App() {
    const [commands, setCommands] = useState<any>([]);
    const [isOpen, setIsOpen] = useState<boolean>(false);
    const [activeChoice, setActiveChoice] = useState<any>(undefined);
    useEffect(() => {
        actionListener.configureUpdateCommands(setCommands);
        actionListener.enableGameInput = !isOpen;
        actionListener.updateSelected(activeChoice);
    });

    console.log(isOpen);

    return (
        <>
            <CommandBar 
                commands={commands}
                open={open}
                isOpen={setIsOpen}
                activeChoice={setActiveChoice}
                commandIssued={(cmd: any) => {
                    processCommand(cmd.choiceSelection, cmd.additionalText);
                }}
            />
            <Viewport />
        </>
    );
}

root.render(<App />);
