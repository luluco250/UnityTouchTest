using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct ListItem {
	public Sprite image;
}

public class ListObject : MonoBehaviour {
	public int index;
}

public class ListManager : MonoBehaviour {
	public RectTransform scrollContent;
	public Text itemText;
	public List<ListItem> itemList = new List<ListItem>();

	string originalItemText;
	Coroutine currentSayMyName;

	public void SortList() {
		itemList.Sort((ListItem a, ListItem b) => {
			return String.Compare(a.image.name, b.image.name);
		});
	}

	public void PopulateList(int quantity) {
		for (int i = 0; i < quantity; ++i)
			itemList.Add(itemList[
				UnityEngine.Random.Range(0, itemList.Count - 1)
			]);
	}

	bool CheckParameters() {
		return scrollContent != null
		    && itemText != null;
	}

	void OnEnable() {
		if (!CheckParameters()) {
			enabled = false;
			return;
		}
	}

	void OnValidate() {
		if (!CheckParameters()) {
			enabled = false;
			return;
		}
	}

	void AddToContent() {
		for (int i = 0; i < itemList.Count; ++i) {
			var item = itemList[i];

			var go = new GameObject(item.image.name);

			var lo = go.AddComponent<ListObject>();
			lo.index = i;

			var img = go.AddComponent<Image>();
			img.sprite = item.image;

			var btn = go.AddComponent<Button>();
			btn.image = img;
			/*btn.onClick.AddListener(() => {
				if (currentSayMyName != null)
					StopCoroutine(currentSayMyName);
				currentSayMyName = StartCoroutine(SayMyName(go));
			});*/
			btn.onClick.AddListener(() => {
				SpawnItem(lo.index);
			});

			go.transform.SetParent(scrollContent, false);
		}
	}

	public IEnumerator SayMyName(GameObject go) {
		var original = itemText.text;
		itemText.text = go.name;

		yield return new WaitForSeconds(1f);

		itemText.text = originalItemText;
		currentSayMyName = null;
	}

	void LoadItems() {
		const string itemsFolder = "./Content/ItemImages/";
		string[] pngFiles = Directory.GetFiles(itemsFolder, "*.png");

		for (int i = 0; i < pngFiles.Length; ++i) {
			var path = pngFiles[i];

			var img = File.ReadAllBytes(path);
			var tex = new Texture2D(
				1, 1,
				TextureFormat.ARGB32,
				false, false
			);
			tex.LoadImage(img);

			var sprite = Sprite.Create(
				tex,
				new Rect(0, 0, tex.width, tex.height),
				new Vector2(0.5f, 0.5f)
			);
			sprite.name = Path.GetFileNameWithoutExtension(path);

			itemList.Add(new ListItem{
				image = sprite
			});
		}
	}

	void SpawnItem(int index) {
		var item = itemList[index];

		var go = new GameObject(item.image.name);
		go.transform.position = Vector3.zero;

		var lo = go.AddComponent<ListObject>();
		lo.index = index;

		var rdr = go.AddComponent<SpriteRenderer>();
		rdr.sprite = item.image;

		var col = go.AddComponent<BoxCollider2D>();

		var mnp = go.AddComponent<Manipulable>();
		mnp.onTap.AddListener((GameObject) => {
			Debug.Log(go.name + " time");
		});
	}

	public void OnTapListItem(GameObject go) {
		var listObj = go.GetComponent<ListObject>();
		if (listObj != null)
			SpawnItem(listObj.index);
	}

	void Start() {
		originalItemText = itemText.text;

		LoadItems();
		PopulateList(100);
		//SortList();
		AddToContent();
	}
}
