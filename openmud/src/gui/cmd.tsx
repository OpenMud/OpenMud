import React from 'react';
import {useState} from "react";
import CommandPalette from 'react-command-palette';

function resolveSeachTest(value: any, commands: any) {
  if(value.length === 0)
    return value;

  const opts = [...(
    commands
      .filter((t: any) => t.name.indexOf(value.substring(0, t.name.length)) === 0)
      .sort((a: any, b: any) => b.name.length - a.name.length)
  )]

  if(opts.length === 0)
    return value;
  else if(opts.length === 1)
    return opts[0].name;

   return opts[0].name.substring(0, value.length);
}

export function CommandBar(props: any) {

  
  const [activeCommand, setActiveCommand] = useState("");

  const theme = {
    overlay: "test_overlay",
    modal: "atom-modal",
    container: "atom-container",
    content: "atom-content",
    containerOpen: "atom-containerOpen",
    input: "atom-input",
    inputOpen: "atom-inputOpen",
    inputFocused: "atom-inputFocused",
    spinner: "atom-spinner",
    suggestionsContainer: "atom-suggestionsContainer",
    suggestionsContainerOpen: "atom-suggestionsContainerOpen",
    suggestionsList: "atom-suggestionsList",
    suggestion: "atom-suggestion",
    suggestionFirst: "atom-suggestionFirst",
    suggestionHighlighted: "atom-suggestionHighlighted",
    trigger: "atom-trigger"
  }
  return (
    <CommandPalette
      trigger={null}
      commands={props.commands}
      hotKeys="tab"
      theme={theme}
      resetInputOnOpen={true}
      filterSearchQuery={(value: any) => {
        setActiveCommand(value);
        return resolveSeachTest(value, props.commands);
      }}
      onRequestClose={() => props.isOpen(false)} onAfterOpen={
        () => {
          setActiveCommand("")
          props.isOpen(true);
        }
      }
      onHighlight={(ch : any) => props.activeChoice(ch)}
      closeOnSelect={true}
      onSelect={(command: any) => {
        props.commandIssued({choiceSelection: command, additionalText: activeCommand.substring(command.name.length)})}}
    />
  );
}