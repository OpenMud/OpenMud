function getClosestDirection(curDir: number, y: string[]): string {
  const n = DirectionName(curDir);
  //y = northeast, n = north
  //y = [6, 1]; n = 0
  const directions = ["north", "east", "south", "west", "southeast", "southwest", "northeast", "northwest"];
  const nIndex = directions.indexOf(n);
  let minDiff = Infinity;
  let closestDirection = "";

  for (const direction of y) {
    const directionIndex = directions.indexOf(direction);
    const diff = Math.abs(directionIndex - nIndex);

    if (diff < minDiff) {
      minDiff = diff;
      closestDirection = direction;
    }
  }

  return closestDirection;
}

function closestDirection(n: string, directions: string[]): string | null {
  if(n == "up")
    n = "north";

  if(n == "down")
    n = "south";

  // Define the possible directions and their corresponding angles
  const allDirections = [
    "north",
    "northeast",
    "east",
    "southeast",
    "south",
    "southwest",
    "west",
    "northwest",
  ];

  const directionAngles = [0, 45, 90, 135, 180, 225, 270, 315];

  // Validate the input direction
  if (!allDirections.includes(n)) {
    throw new Error("Invalid input direction");
  }

  // Calculate the angle of the input direction
  const nAngle = directionAngles[allDirections.indexOf(n)];

  // Calculate the closest direction by finding the direction with the smallest angle difference
  let closestDirection: string | null = null;
  let minAngleDifference = 360; // Initialize with a large value

  for (const direction of directions) {
    if (!allDirections.includes(direction)) {
      throw new Error("Invalid direction in the list");
    }

    const directionAngle = directionAngles[allDirections.indexOf(direction)];
    const angleDifference = Math.abs(nAngle - directionAngle);

    // Check if this direction has a smaller angle difference
    if (angleDifference < minAngleDifference) {
      minAngleDifference = angleDifference;
      closestDirection = direction;
    }
  }

  return closestDirection;
}
function DirectionName(n: number): string {
  switch (n) {
    default:
    case 1: return 'north';
    case 2: return 'south';
    case 4: return 'east';
    case 8: return 'west';
    case 5: return 'northeast';
    case 9: return 'northwest';
    case 6: return 'southeast';
    case 10: return 'up';
    case 32: return 'down';
  }
}

function ResolveAnimationName(gameIconAnimationsIndex: any, iconName: string, stateName: string, currentDirection: number | undefined = undefined): string | undefined {

  const fieldName = iconName as keyof typeof gameIconAnimationsIndex;
  const animations = gameIconAnimationsIndex[fieldName];

  if (!animations.find((v: any) => v.startsWith(stateName)))
    stateName = animations[0].substr(0, animations[0].lastIndexOf('_'));

  const stateAnimations = [...gameIconAnimationsIndex[fieldName].filter( (v:any) => v.startsWith(stateName))];
  const possibleDirections: string[] = [...stateAnimations.map(s => s.split('_').pop() as string)];

  if (possibleDirections.length == 0)
    return stateName;

  const currentDirectionName = DirectionName(currentDirection ?? 1);

  var dir = closestDirection(currentDirectionName, possibleDirections);

  const resultant = stateName + '_' + dir;

  if (!animations.includes(resultant)) {
    console.log("Error, animation not present.");
    return undefined;
  }

  return resultant;
}

export { ResolveAnimationName };
