# PointCloudViewer

これは .pts 形式で作成されたPoint cloudデータをUnity上で閲覧するためのスクリプトと、そのUnityプロジェクトです。
Taskを使っているので、非対応のUnityバージョンでは利用できません。

作成時は2018.3.8f1を使用しています。

動作の保証等は致しませんが、私的利用の範囲内でご自由にご利用ください。

# HOW TO USE

#### English

1. Open scene `cubing` in `Assets/Scenes` and select `Arranging` object on hierarchy.
2. Set path is target `.pts` file to `File Path` in Script `Pts To Cubing Manager` script.
3. Set `Cube size` value in `Pts To Cubing Manager` script. 
   + this value is used to unite points and generating cube, So if this value is smaller source points's offset, generated cubes have gaps between each other.
4. Set `Chunk Size` value in `Points Arranger` script.
   + this value is used to chunking blocked points.If you set `30` to this value, blocked points are chunked into 30 x 30 x 30 (meters) block.
5. Check and set `Size Scale` value in `PtsToCloudPointConverter` script.
   + this value is used to converting unit, So if source points have milli meter value, is set this `0.001`.
6. Set `Axis` in `Pts To Cloud Point Converter` script.
7. Check and set `Max Thread Num` value in  `Pts To Cubing Manager` and `Points arranger`, `Pts To Cloud Point Converter`, `Points To Cube`, `Points Slicer`, `Points Collector`.
8. Check all other variables and their values and set if undefined.

#### Japanese

1. `Assets/Scenes` ディレクトリ内の `cubing` シーンを開きヒエラルキーから `Arranging` オブジェクトを選択します。
2. `Pts To Cubing Manager` スクリプトにある `File Path` に変換対象になる `.pts` ファイルのパスを設定します。
3. `Pts To Cubing Manager` スクリプトにある `Cube Size` の値を設定します。
   + この値は点をまとめてCube化する際に使用するので、この値が元の点群データのオフセットよりも小さい場合、生成されるCube群の間に隙間が空きます。
4. `Points Arranger` スクリプトにある `Chunk Size`　の値を設定します
   +  この値はキューブ化された点をチャンク化する際の範囲として利用されます。もし値が `30` に設定された場合、チャンクは 30 x 30 x 30 (メートル)のブロックとして設定されます。
5. `Pts To Cloud Point Converter` スクリプトにある `Size Scale` の値を確認し、設定してください。
   + この値は元となる点群データからの単位の変換に使われます。もし元のデータがミリメートル単位で座標を保持するのであれば、この値は `0.001` になります。
6. `Pts To Cloud Point Converter` にある `Axis` の値を設定します。
7. `Pts To Cubing Manager` および  `Points arranger`, `Pts To Cloud Point Converter`, `Points To Cube`, `Points Slicer`, `Points Collector` にある `Max Thread` の値を確認し、設定します。
8. そのほかのすべての変数およびその値を確認し、設定します。