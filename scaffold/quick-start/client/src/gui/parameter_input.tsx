import * as React from 'react';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog/Dialog';
import DialogTitle from '@mui/material/DialogTitle/DialogTitle';
import DialogContent from '@mui/material/DialogContent/DialogContent';
import DialogContentText from '@mui/material/DialogContentText/DialogContentText';
import DialogActions from '@mui/material/DialogActions/DialogActions';
import TextField from '@mui/material/TextField/TextField';



export function ParameterInput() {
  const [open, setOpen] = React.useState(false);

  const handleClickOpen = () => {
    setOpen(true);
  };

  const handleClose = () => {
    setOpen(false);
  };

  return (
    <div>
      <Button variant="outlined" onClick={handleClickOpen}>
        Open alert dialog
      </Button>
      <Dialog
        open={open}
        onClose={handleClose}
        aria-labelledby="alert-dialog-title"
        aria-describedby="alert-dialog-description"
        PaperProps={{
          style: {
            borderColor: 'black',
            boxShadow: 'none',
            position: "absolute",
            left: 0,
            bottom: 0
          },
        }}
        BackdropProps={{
          style: {
            backgroundColor: 'transparent',
            boxShadow: 'none'
          },
        }}
      >
        <DialogContent>
          <DialogContentText id="alert-dialog-description">
            <TextField id="outlined-basic" label="Message" variant="outlined" />
          </DialogContentText>
        </DialogContent>
      </Dialog>
    </div>
  );
}