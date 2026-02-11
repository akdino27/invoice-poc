import { Directive, EventEmitter, HostListener, Output, HostBinding } from '@angular/core';

@Directive({
  selector: '[appDragDrop]',
  standalone: true
})
export class DragDropDirective {
  @Output() filesDropped = new EventEmitter<FileList>();
  @HostBinding('class.drag-over') isDragOver = false;

  @HostListener('dragover', ['$event'])
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  @HostListener('dragleave', ['$event'])
  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  @HostListener('drop', ['$event'])
  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.filesDropped.emit(files);
    }
  }
}
