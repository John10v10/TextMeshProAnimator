/*	TextMeshPro Animator v0.3 by John10v10 (with some by Meorge)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

public enum TextMeshAnimator_IndependencyType{
	United,
	Word,
	Character,
	Vertex
}
[RequireComponent(typeof(TextMeshProUGUI))]
public class TextMeshAnimator : MonoBehaviour {

	public int currentFrame = 0;
	public bool useCustomText = false;
	public string customText;
	public string text {
		get{ return customText; }
		set{ customText = value;
			//UpdateText(value);
			if (useCustomText) {
				TMProGUI.text = ParseText(value);
				//charsVisible = 0;
				SyncToTextMesh ();
			}
		}
	}

	public int totalChars {
		get {
			return TMProGUI.textInfo.characterCount;
		}
	}
	//MODIFIER VARIABLES

	//SHAKE
	public float shakeAmount = 1;
	public TextMeshAnimator_IndependencyType shakeIndependency = TextMeshAnimator_IndependencyType.Character;

	//WAVE
	public float waveAmount = 1;
	public float waveSpeed = 1;
	public float waveSeparation = 1;
	public TextMeshAnimator_IndependencyType waveIndependency = TextMeshAnimator_IndependencyType.Vertex;

	//WIGGLE
	public float wiggleAmount = 1;
	public float wiggleSpeed = 0.125f;
	public float wiggleMinimumDuration = 0.5f;
	public TextMeshAnimator_IndependencyType wiggleIndependency = TextMeshAnimator_IndependencyType.Character;

	private TextMeshProUGUI TMProGUI;
	// Use this for initialization

	private Vector3[][] vertex_Base; // The base vertex array the animator will animate from.

	//PRIVATE TEXT CACHE VARIABLES

	//SHAKE
	private bool[] shakesEnabled;
	private float[] shakeVelocities;
	private TextMeshAnimator_IndependencyType[] shakeIndependencies;

	//WAVE
	private bool[] wavesEnabled;
	private float[] waveVelocities;
	private float[] waveSpeeds;
	private float[] waveSeparations;
	private TextMeshAnimator_IndependencyType[] waveIndependencies;

	//WIGGLE
	private bool[] wigglesEnabled;
	private float[] wiggleVelocities;
	private float[] wiggleSpeeds;
	private float[] wigglePrevPos;
	private float[] wiggleTargetPos;
	private float[] wiggleTimeCurrent;
	private float[] wiggleTimeTotal;
	private TextMeshAnimator_IndependencyType[] wiggleIndependencies;

	//SHOW TEXT
	[SerializeField]
	public int charsVisible;

	[SerializeField]
	public int[] scrollSpeeds;

	// Custom opener/closers
	[SerializeField]
	public char openingChar = '<';
	[SerializeField]
	public char closingChar = '>';


	public struct TextSpeedItem {
		public int speed;
		public int index;
	}

	public void SyncToTextMesh (){
		TMProGUI.ForceMeshUpdate ();
		vertex_Base = new Vector3[TMProGUI.textInfo.meshInfo.Length][];
		int biggest_num_verts = 0;
		for (int i = 0; i < TMProGUI.textInfo.meshInfo.Length; ++i) {
			vertex_Base [i] = new Vector3[TMProGUI.textInfo.meshInfo [i].vertices.Length];
			if(biggest_num_verts < vertex_Base [i].Length)biggest_num_verts=vertex_Base [i].Length;
			System.Array.Copy (TMProGUI.textInfo.meshInfo [i].vertices, vertex_Base [i], TMProGUI.textInfo.meshInfo [i].vertices.Length);
		}
		wigglePrevPos = new float[biggest_num_verts * 2];
		wiggleTargetPos = new float[biggest_num_verts * 2];
		wiggleTimeCurrent = new float[biggest_num_verts * 2];
		wiggleTimeTotal = new float[biggest_num_verts * 2];
		TMProGUI.ForceMeshUpdate ();
	}

	public void UpdateText(string text = null){
		//charsVisible = 0;
		if(TMProGUI == null)TMProGUI = gameObject.GetComponent<TextMeshProUGUI> ();
		if (useCustomText){
			if(text == null)this.text = this.text;
			else this.text = text;
		}
		else {
			TMProGUI.text = ParseText (TMProGUI.text);SyncToTextMesh ();
		}
	}

	string ParseText(string inputText){

		//SHAKE
		List<bool> shakesEnabled = new List<bool> ();
		List<float> shakeVelocities = new List<float> ();
		List<TextMeshAnimator_IndependencyType> shakeIndependencies = new List<TextMeshAnimator_IndependencyType> ();
		bool shaking = false;
		float shakeAmount = 1;
		TextMeshAnimator_IndependencyType shakeIndependency = this.shakeIndependency;

		//WAVE
		List<bool> wavesEnabled = new List<bool> ();
		List<float> waveVelocities = new List<float> ();
		List<float> waveSpeeds = new List<float> ();
		List<float> waveSeparations = new List<float> ();
		List<TextMeshAnimator_IndependencyType> waveIndependencies = new List<TextMeshAnimator_IndependencyType> ();
		bool waving = false;
		float waveAmount = 1;
		float waveSpeed = 1;
		float waveSeparation = 1;
		TextMeshAnimator_IndependencyType waveIndependency = this.waveIndependency;

		//WIGGLE
		List<bool> wigglesEnabled = new List<bool> ();
		List<float> wiggleVelocities = new List<float> ();
		List<float> wiggleSpeeds = new List<float> ();
		List<TextMeshAnimator_IndependencyType> wiggleIndependencies = new List<TextMeshAnimator_IndependencyType> ();
		bool wiggling = false;
		float wiggleAmount = 1;
		float wiggleSpeed = 1;
		TextMeshAnimator_IndependencyType wiggleIndependency = this.wiggleIndependency;

		// SCROLL SPEED
		List<int> scrollSpeeds = new List<int>();
		int currentScrollSpeed = 2;


		string outputText = "";
		for (int index = 0; index < inputText.Length; index++) {
			if (inputText [index] == openingChar) {
				int startTagIndex = index;
				while (index < inputText.Length) {

					if (inputText [index++] == closingChar) {
						string tag = inputText.Substring (startTagIndex, index - startTagIndex);
						Debug.Log("TAG FOUND: " + tag);
						if (tag.ToUpper().Contains("COLOR") || tag.ToUpper().Contains("SIZE") || tag.ToUpper() == openingChar + "B" + closingChar || tag.ToUpper() == openingChar + "/B" + closingChar || tag.ToUpper() == openingChar + "I" + closingChar || tag.ToUpper() == openingChar + "/I" + closingChar) {
							Debug.Log("This is a rich-text tag, don't worry about it");
							if (openingChar != '<' || closingChar != '>') {
								Debug.LogWarning("These are not normal TextMeshPro tags, so they'll likely show up in the box.");
							}
							outputText += tag;

						}
						//SHAKE

						else if (tag.ToUpper ().Contains ("/SHAKE")) {
							shaking = false;
							shakeAmount = 1;
						}
						else if (tag.ToUpper ().Contains ("SHAKE")) {
							shaking = true;

							//INTENSITY

							string amountLabel = "INTENSITY=";
							if (tag.ToUpper ().Contains (amountLabel)) {
								int startIndex = tag.ToUpper ().IndexOf (amountLabel) + amountLabel.Length;
								int iiii = startIndex;
								for(; iiii < tag.Length; iiii++)
									{
										if (!char.IsDigit(tag[iiii]) && (tag[iiii] != '.'))
											break;
									}
									string amount_string = tag.Substring (startIndex, iiii-startIndex);
								if (!float.TryParse (amount_string, out shakeAmount)) {
									Debug.LogError (string.Format ("'{0}' is not a valid value for shake amount.", amount_string));
								}
							}
							if (tag.ToUpper ().Contains ("UNITED")) {
								shakeIndependency = TextMeshAnimator_IndependencyType.United;
							}
							if (tag.ToUpper ().Contains ("WORD")) {
								shakeIndependency = TextMeshAnimator_IndependencyType.Word;
							}
							if (tag.ToUpper ().Contains ("CHARACTER")) {
								shakeIndependency = TextMeshAnimator_IndependencyType.Character;
							}
							if (tag.ToUpper ().Contains ("VERTEX")) {
								shakeIndependency = TextMeshAnimator_IndependencyType.Vertex;
							}
						}

						//WAVE

						else if (tag.ToUpper ().Contains ("/WAVE")) {
							waving = false;
							waveAmount = 1;
							waveSpeed = 1;
							waveSeparation = 1;
						}
						else if (tag.ToUpper ().Contains ("WAVE")) {
							waving = true;

							//INTENSITY

							string amountLabel = "INTENSITY=";
							if (tag.ToUpper ().Contains (amountLabel)) {
								int startIndex = tag.ToUpper ().IndexOf (amountLabel) + amountLabel.Length;
								int iiii = startIndex;
								for(; iiii < tag.Length; iiii++)
								{
									if (!char.IsDigit(tag[iiii]) && (tag[iiii] != '.'))
										break;
								}
								string amount_string = tag.Substring (startIndex, iiii-startIndex);
								if (!float.TryParse (amount_string, out waveAmount)) {
									Debug.LogError (string.Format ("'{0}' is not a valid value for wave amount.", amount_string));
								}
							}

							//SPEED

							string speedLabel = "SPEED=";
							if (tag.ToUpper ().Contains (speedLabel)) {
								int startIndex = tag.ToUpper ().IndexOf (speedLabel) + speedLabel.Length;
								int iiii = startIndex;
								for(; iiii < tag.Length; iiii++)
								{
									if (!char.IsDigit(tag[iiii]) && (tag[iiii] != '.'))
										break;
								}
								string speed_string = tag.Substring (startIndex, iiii-startIndex);
								if (!float.TryParse (speed_string, out waveSpeed)) {
									Debug.LogError (string.Format ("'{0}' is not a valid value for wave speed.", speed_string));
								}
							}

							//SEPARATION

							string separationLabel = "SEPARATION=";
							if (tag.ToUpper ().Contains (separationLabel)) {
								int startIndex = tag.ToUpper ().IndexOf (separationLabel) + separationLabel.Length;
								int iiii = startIndex;
								for(; iiii < tag.Length; iiii++)
								{
									if (!char.IsDigit(tag[iiii]) && (tag[iiii] != '.'))
										break;
								}
								string separation_string = tag.Substring (startIndex, iiii-startIndex);
								if (!float.TryParse (separation_string, out waveSeparation)) {
									Debug.LogError (string.Format ("'{0}' is not a valid value for wave separation.", separation_string));
								}
							}

							if (tag.ToUpper ().Contains ("UNITED")) {
								waveIndependency = TextMeshAnimator_IndependencyType.United;
							}
							if (tag.ToUpper ().Contains ("WORD")) {
								waveIndependency = TextMeshAnimator_IndependencyType.Word;
							}
							if (tag.ToUpper ().Contains ("CHARACTER")) {
								waveIndependency = TextMeshAnimator_IndependencyType.Character;
							}
							if (tag.ToUpper ().Contains ("VERTEX")) {
								waveIndependency = TextMeshAnimator_IndependencyType.Vertex;
							}
						}

						//WIGGLE

						else if (tag.ToUpper ().Contains ("/WIGGLE")) {
							wiggling = false;
							wiggleAmount = 1;
							wiggleSpeed = 1;
						}
						else if (tag.ToUpper ().Contains ("WIGGLE")) {
							wiggling = true;

							//INTENSITY

							string amountLabel = "INTENSITY=";
							if (tag.ToUpper ().Contains (amountLabel)) {
								int startIndex = tag.ToUpper ().IndexOf (amountLabel) + amountLabel.Length;
								int iiii = startIndex;
								for(; iiii < tag.Length; iiii++)
								{
									if (!char.IsDigit(tag[iiii]) && (tag[iiii] != '.'))
										break;
								}
								string amount_string = tag.Substring (startIndex, iiii-startIndex);
								if (!float.TryParse (amount_string, out wiggleAmount)) {
									Debug.LogError (string.Format ("'{0}' is not a valid value for wiggle amount.", amount_string));
								}
							}

							//SPEED

							string speedLabel = "SPEED=";
							if (tag.ToUpper ().Contains (speedLabel)) {
								int startIndex = tag.ToUpper ().IndexOf (speedLabel) + speedLabel.Length;
								int iiii = startIndex;
								for(; iiii < tag.Length; iiii++)
								{
									if (!char.IsDigit(tag[iiii]) && (tag[iiii] != '.'))
										break;
								}
								string speed_string = tag.Substring (startIndex, iiii-startIndex);
								if (!float.TryParse (speed_string, out wiggleSpeed)) {
									Debug.LogError (string.Format ("'{0}' is not a valid value for wiggle speed.", speed_string));
								}
							}

							if (tag.ToUpper ().Contains ("UNITED")) {
								wiggleIndependency = TextMeshAnimator_IndependencyType.United;
							}
							if (tag.ToUpper ().Contains ("WORD")) {
								wiggleIndependency = TextMeshAnimator_IndependencyType.Word;
							}
							if (tag.ToUpper ().Contains ("CHARACTER")) {
								wiggleIndependency = TextMeshAnimator_IndependencyType.Character;
							}
							if (tag.ToUpper ().Contains ("VERTEX")) {
								wiggleIndependency = TextMeshAnimator_IndependencyType.Vertex;
							}
						}

						// SCROLL SPEED
						else if (tag.ToUpper().Contains("/SPEED")) {
							currentScrollSpeed = 3;
						} else if (tag.ToUpper().Contains("SPEED")) {
							string speedLabel = "AMT=";
							int startIndex = tag.ToUpper ().IndexOf (speedLabel) + speedLabel.Length;
							int iiii = startIndex;
							for(; iiii < tag.Length; iiii++)
							{
								if (!char.IsDigit(tag[iiii]) && (tag[iiii] != '.'))
									break;
							}
							string speed_string = tag.Substring (startIndex, iiii-startIndex);

							currentScrollSpeed = int.Parse(speed_string);
						}
						
						break;
					}
				}
			}
			if (index >= inputText.Length)
				continue;
			if (!char.IsControl(inputText [index]) && (inputText [index] != ' ')) {

				//SHAKE

				shakesEnabled.Add (shaking);
				shakeVelocities.Add (shakeAmount);
				shakeIndependencies.Add (shakeIndependency);


				//WAVE

				wavesEnabled.Add (waving);
				waveVelocities.Add (waveAmount);
				waveSpeeds.Add (waveSpeed);
				waveSeparations.Add (waveSeparation);
				waveIndependencies.Add (waveIndependency);

				//WIGGLE

				wigglesEnabled.Add (wiggling);
				wiggleVelocities.Add (wiggleAmount);
				wiggleSpeeds.Add (wiggleSpeed);
				wiggleIndependencies.Add (wiggleIndependency);

				scrollSpeeds.Add(currentScrollSpeed);


			}

			outputText += inputText [index];
		}

		//SHAKE

		this.shakesEnabled = shakesEnabled.ToArray ();
		this.shakeVelocities = shakeVelocities.ToArray ();
		this.shakeIndependencies = shakeIndependencies.ToArray ();

		//WAVE

		this.wavesEnabled = wavesEnabled.ToArray ();
		this.waveVelocities = waveVelocities.ToArray ();
		this.waveSpeeds = waveSpeeds.ToArray ();
		this.waveSeparations = waveSeparations.ToArray ();
		this.waveIndependencies = waveIndependencies.ToArray ();

		//WIGGLE

		this.wigglesEnabled = wigglesEnabled.ToArray ();
		this.wiggleVelocities = wiggleVelocities.ToArray ();
		this.wiggleSpeeds = wiggleSpeeds.ToArray ();
		this.wiggleIndependencies = wiggleIndependencies.ToArray ();

		// SCROLL SPEED
		this.scrollSpeeds = scrollSpeeds.ToArray();
		
		return outputText;
	}

	void Start () {
		UpdateText();
	}

	public void BeginAnimation (string text = null){
		UpdateText(text);
		currentFrame = 0;
	}
	//SHAKE
	Vector3 ShakeVector(float amount){
		return new Vector3(Random.Range(-amount,amount),Random.Range(-amount,amount));
	}
	//WAVE
	Vector3 WaveVector(float amount, float time){
		return new Vector3(0,Mathf.Sin(time)*amount);
	}
	//WIGGLE
	Vector3 WiggleVector(float amount, float speed, ref int i){
		wiggleTimeCurrent[i*2] += speed;

		if ((wiggleTimeTotal[i*2] == 0)||(wiggleTimeCurrent[i*2] / wiggleTimeTotal[i*2]) >= 1) {
			wiggleTimeCurrent[i*2] -= wiggleTimeTotal[i*2];
			wiggleTimeTotal[i*2] = Random.Range(wiggleMinimumDuration,1.0f);
			wigglePrevPos[i*2] = wiggleTargetPos[i*2];
			wiggleTargetPos[i*2] = Random.Range(-amount, amount);
		}
		wiggleTimeCurrent[i*2+1] += speed;
		if ((wiggleTimeTotal[i*2+1] == 0)||(wiggleTimeCurrent[i*2+1] / wiggleTimeTotal[i*2+1]) >= 1) {
			wiggleTimeCurrent[i*2+1] -= wiggleTimeTotal[i*2+1];
			wiggleTimeTotal[i*2+1] = Random.Range(wiggleMinimumDuration,1.0f);
			wigglePrevPos[i*2+1] = wiggleTargetPos[i*2+1];
			wiggleTargetPos[i*2+1] = Random.Range(-amount, amount);
		}
		Vector3 outputVector = new Vector3 (Mathf.SmoothStep(wigglePrevPos[i*2], wiggleTargetPos[i*2], (wiggleTimeCurrent[i*2] / wiggleTimeTotal[i*2])),Mathf.SmoothStep(wigglePrevPos[i*2+1], wiggleTargetPos[i*2+1], (wiggleTimeCurrent[i*2+1] / wiggleTimeTotal[i*2+1])));

		++i;
		return outputVector;
	}

	// Update is called once per frame
	void Update () {
		
		Vector3 sv = new Vector3(); //SHAKE

		Vector3 wv = new Vector3(); //WAVE

		Vector3 wgv = new Vector3(); //WIGGLE

		for (int i = 0; i < TMProGUI.textInfo.meshInfo.Length; ++i) {
			int j = 0;

			//SHAKE


			float shakeAmount = 1;
			if (shakeVelocities.Length > j){
				shakeAmount = shakeVelocities[j];
			};
			int sl = 0;
			TextMeshAnimator_IndependencyType shakeIndependency = this.shakeIndependency;
			if (shakeIndependency == TextMeshAnimator_IndependencyType.United) sv = ShakeVector(this.shakeAmount);

			//WAVE

			float waveAmount = 1;
			if (waveVelocities.Length > j){
				waveAmount = waveVelocities[j];
			};

			float waveSpeed = 1;
			if (waveSpeeds.Length > j){
				waveSpeed = waveSpeeds[j];
			};
			int wl = 0;
			TextMeshAnimator_IndependencyType waveIndependency = this.waveIndependency;
			if (waveIndependency == TextMeshAnimator_IndependencyType.United) wv = WaveVector(this.waveAmount, currentFrame*(this.waveSpeed*waveSpeed));


			//WIGGLE

			float wiggleAmount = 1;
			if (wiggleVelocities.Length > j){
				wiggleAmount = wiggleVelocities[j];
			};

			float wiggleSpeed = 1;
			if (wiggleSpeeds.Length > j){
				wiggleSpeed = wiggleSpeeds[j];
			};
			int wgl = 0;
			int wgll = 0;
			TextMeshAnimator_IndependencyType wiggleIndependency = this.wiggleIndependency;
			if (wiggleIndependency == TextMeshAnimator_IndependencyType.United) wgv = WiggleVector(this.wiggleAmount, this.wiggleSpeed*wiggleSpeed, ref wgll);

			for (int v = 0; v < TMProGUI.textInfo.meshInfo [i].vertices.Length; v += 4, ++j) {

				for (byte k = 0; k < 4; ++k)
					TMProGUI.textInfo.meshInfo [i].vertices [v + k] = vertex_Base [i] [v + k];

				//SHAKE

				TextMeshAnimator_IndependencyType prevShakeIndependency = shakeIndependency;
				if (j < shakeIndependencies.Length) {
					shakeIndependency = shakeIndependencies [j];
				}
				if ((j >= 1) && (j < shakeIndependencies.Length + 1)) {
					prevShakeIndependency = shakeIndependencies [j-1];
				}
				if (shakeIndependency == TextMeshAnimator_IndependencyType.Word) {
					if (sl < TMProGUI.text.Length) {
						if ((TMProGUI.text [sl] == ' ') || char.IsControl (TMProGUI.text [sl]) || (prevShakeIndependency != TextMeshAnimator_IndependencyType.Word) || (sl==0)) {
							sv = ShakeVector (this.shakeAmount);
							if ((TMProGUI.text [sl] == ' ') || char.IsControl (TMProGUI.text [sl])) ++sl;
						}
					}
				}
				++sl;
				bool shake = false;
				if (shakesEnabled.Length > j){
					shake = shakesEnabled[j];
				}
				if(shake){
					if (shakeVelocities.Length > j){
						shakeAmount = shakeVelocities[j];
					};
					if(shakeIndependency == TextMeshAnimator_IndependencyType.Character)sv = ShakeVector(this.shakeAmount);
					for (byte k = 0; k < 4; ++k) {
						if(shakeIndependency == TextMeshAnimator_IndependencyType.Vertex)sv = ShakeVector(this.shakeAmount);
						TMProGUI.textInfo.meshInfo [i].vertices [v + k] += sv * shakeAmount;
					}
				}

				//WAVE
				if (waveSpeeds.Length > j){
					waveSpeed = waveSpeeds[j];
				};

				float waveSeparation = this.waveSeparation;
				if (waveSeparations.Length > j){
					waveSeparation = waveSeparations[j];
				};

				TextMeshAnimator_IndependencyType prevWaveIndependency = waveIndependency;
				if (j < waveIndependencies.Length) {
					waveIndependency = waveIndependencies [j];
				}
				if ((j >= 1) && (j < waveIndependencies.Length + 1)) {
					prevWaveIndependency = waveIndependencies [j-1];
				}
				if (waveIndependency == TextMeshAnimator_IndependencyType.Word) {
					if (wl < TMProGUI.text.Length) {
						if ((TMProGUI.text [wl] == ' ') || char.IsControl (TMProGUI.text [wl]) || (prevWaveIndependency != TextMeshAnimator_IndependencyType.Word) || (wl==0)) {
							wv = WaveVector(this.waveAmount, currentFrame*(this.waveSpeed*waveSpeed)+this.waveSpeed*waveSpeed+TMProGUI.textInfo.meshInfo [i].vertices [v].x/(this.waveSeparation*waveSeparation));
							if ((TMProGUI.text [wl] == ' ') || char.IsControl (TMProGUI.text [wl])) ++wl;
						}
					}
				}
				++wl;

				bool wave = false;
				if (wavesEnabled.Length > j){
					wave = wavesEnabled[j];
				}
				if(wave){
					if (waveVelocities.Length > j){
						waveAmount = waveVelocities[j];
					};
					if(waveIndependency == TextMeshAnimator_IndependencyType.Character)wv = WaveVector(this.waveAmount, currentFrame*(this.waveSpeed*waveSpeed)+TMProGUI.textInfo.meshInfo [i].vertices [v].x/(this.waveSeparation*waveSeparation));
					for (byte k = 0; k < 4; ++k) {
						if(waveIndependency == TextMeshAnimator_IndependencyType.Vertex)wv = WaveVector(this.waveAmount, currentFrame*(this.waveSpeed*waveSpeed)+TMProGUI.textInfo.meshInfo [i].vertices [v + k].x/(this.waveSeparation*waveSeparation));
						TMProGUI.textInfo.meshInfo [i].vertices [v + k] += wv * waveAmount;
					}
				}

				//WIGGLE

				wiggleSpeed = this.wiggleSpeed;
				if (wiggleSpeeds.Length > j){
					wiggleSpeed = wiggleSpeeds[j];
				};

				TextMeshAnimator_IndependencyType prevwiggleIndependency = wiggleIndependency;
				if (j < wiggleIndependencies.Length) {
					wiggleIndependency = wiggleIndependencies [j];
				}
				if ((j >= 1) && (j < wiggleIndependencies.Length + 1)) {
					prevwiggleIndependency = wiggleIndependencies [j-1];
				}
				if (wiggleIndependency == TextMeshAnimator_IndependencyType.Word) {
					if (wgl < TMProGUI.text.Length) {
						if ((TMProGUI.text [wgl] == ' ') || char.IsControl (TMProGUI.text [wgl]) || (prevwiggleIndependency != TextMeshAnimator_IndependencyType.Word) || (wgl==0)) {
							wgv = WiggleVector(this.wiggleAmount, this.wiggleSpeed*wiggleSpeed, ref wgll);
							if ((TMProGUI.text [wgl] == ' ') || char.IsControl (TMProGUI.text [wgl])) ++wgl;
						}
					}
				}
				++wgl;

				bool wiggle = false;
				if (wigglesEnabled.Length > j){
					wiggle = wigglesEnabled[j];
				}
				if(wiggle){
					if (wiggleVelocities.Length > j){
						wiggleAmount = wiggleVelocities[j];
					};
					if (wiggleIndependency == TextMeshAnimator_IndependencyType.Character) {
						wgv = WiggleVector (this.wiggleAmount, this.wiggleSpeed*wiggleSpeed, ref wgll);
					}
					for (byte k = 0; k < 4; ++k) {
						if(wiggleIndependency == TextMeshAnimator_IndependencyType.Vertex)wgv = WiggleVector(this.wiggleAmount, this.wiggleSpeed*wiggleSpeed, ref wgll);
						TMProGUI.textInfo.meshInfo [i].vertices [v + k] += wgv * wiggleAmount;
					}
				}


				// CHAR VISIBILITY
				if ((v / 4) + 1 > charsVisible) {
					for (int g = 0; g < 4; g++) {
						TMProGUI.textInfo.meshInfo[i].vertices[v+g] = Vector3.zero;

						//Color32 currentColor = TMProGUI.textInfo.characterInfo[(v/4)+1].color;
						//TMProGUI.textInfo.characterInfo[(v/4)+1].color = new Color32(currentColor.r, currentColor.g, currentColor.b, (byte)0);
						
					}
				}
			}
			
			TMProGUI.UpdateVertexData();
			//TMProGUI.ForceMeshUpdate();
		}
		++currentFrame;
	}
}
[CustomEditor(typeof(TextMeshAnimator))]
public class TextMeshAnimatorEditor : Editor 
{
	SerializedProperty useCustomText;
	SerializedProperty customText;
	SerializedProperty shakeAmount;
	SerializedProperty shakeIndependency;
	SerializedProperty waveAmount;
	SerializedProperty waveSpeed;
	SerializedProperty waveSeparation;
	SerializedProperty waveIndependency;
	SerializedProperty wiggleAmount;
	SerializedProperty wiggleSpeed;
	SerializedProperty wiggleMinimumDuration;
	SerializedProperty wiggleIndependency;
	SerializedProperty charsVisible;
	SerializedProperty openingChar, closingChar;

	void OnEnable()
	{
		useCustomText = serializedObject.FindProperty("useCustomText");
		customText = serializedObject.FindProperty("customText");
		shakeAmount = serializedObject.FindProperty("shakeAmount");
		shakeIndependency = serializedObject.FindProperty("shakeIndependency");
		waveAmount = serializedObject.FindProperty("waveAmount");
		waveSpeed = serializedObject.FindProperty("waveSpeed");
		waveSeparation = serializedObject.FindProperty("waveSeparation");
		waveIndependency = serializedObject.FindProperty("waveIndependency");
		wiggleAmount = serializedObject.FindProperty("wiggleAmount");
		wiggleSpeed = serializedObject.FindProperty("wiggleSpeed");
		wiggleMinimumDuration = serializedObject.FindProperty("wiggleMinimumDuration");
		wiggleIndependency = serializedObject.FindProperty("wiggleIndependency");

		charsVisible = serializedObject.FindProperty("charsVisible");

		openingChar = serializedObject.FindProperty("openingChar");
		closingChar = serializedObject.FindProperty("closingChar");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update ();
		if (useCustomText.boolValue = EditorGUILayout.Toggle ("Custom Text", useCustomText.boolValue)) {
			customText.stringValue = EditorGUILayout.TextArea (customText.stringValue, GUILayout.Height (96));
			
		}
		if (GUILayout.Button ("Update")) {
			TextMeshAnimator script = (TextMeshAnimator)target;
			script.UpdateText ();
		}

		EditorGUILayout.Space ();

		EditorGUILayout.LabelField("Opening/Closing Characters", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField(openingChar);
		EditorGUILayout.PropertyField(closingChar);
		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Text Visibility Properties", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField (charsVisible);
		EditorGUILayout.Space();

		EditorGUILayout.LabelField ("Shake Properties", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField (shakeAmount);
		EditorGUILayout.PropertyField (shakeIndependency);

		EditorGUILayout.Space ();

		EditorGUILayout.LabelField ("Wave Properties", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField (waveAmount);
		EditorGUILayout.PropertyField (waveSpeed);
		EditorGUILayout.PropertyField (waveSeparation);
		EditorGUILayout.PropertyField (waveIndependency);

		EditorGUILayout.Space ();

		EditorGUILayout.LabelField ("Wiggle Properties", EditorStyles.boldLabel);
		EditorGUILayout.PropertyField (wiggleAmount);
		EditorGUILayout.PropertyField (wiggleSpeed);
		wiggleMinimumDuration.floatValue = EditorGUILayout.Slider ("Wiggle Minimum Duration", wiggleMinimumDuration.floatValue, 0.0f, 1.0f);
		EditorGUILayout.PropertyField (wiggleIndependency);

		serializedObject.ApplyModifiedProperties ();
	}
}

//hi skawo :>