import { NetComponent } from './NetComponent';

export class NetDeletedComponent extends NetComponent<'deleted'> {
  public readonly type = 'deleted';

  constructor(public readonly frame: number) {
    super();
  }
}
